using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;

namespace CafeManagementSystem.Controllers;

[Authorize(Roles = "Cashier,Admin")]
public class CashierController : Controller
{
    private readonly CafeManagementDbContext _context;
    private const string PosCartSessionKey = "PosCartSession";
    private const string PosCustomerPhoneKey = "PosCustomerPhone";

    public CashierController(CafeManagementDbContext context)
    {
        _context = context;
    }

    // =========================================================================
    // 1. QUẢN LÝ CA THU NGÂN & BÀN GIAO (Shift - GET / OpenShift - POST / CloseShift - POST)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> Shift()
    {
        var cashierId = GetCurrentUserId();

        // 1. Tìm ca Open hiện tại của Cashier
        var currentShift = await _context.CashierShifts
            .Include(s => s.Cashier)
            .Include(s => s.PreviousShift)
            .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.ShiftStatus == "Open");

        // 2. Tìm ca đã đóng gần nhất trong hệ thống (Previous Shift Information)
        var lastClosedShift = await _context.CashierShifts
            .Include(s => s.Cashier)
            .Where(s => s.ShiftStatus == "Closed" || s.ShiftStatus == "HandedOver")
            .OrderByDescending(s => s.EndTime ?? s.StartTime)
            .FirstOrDefaultAsync();

        var viewModel = new CashierShiftViewModel
        {
            HasOpenShift = currentShift != null,
            CurrentShift = currentShift,
            PreviousShift = currentShift?.PreviousShift ?? lastClosedShift,
            LastClosedShift = lastClosedShift,
            InputOpeningCash = lastClosedShift?.ClosingActualCash ?? 1000000m
        };

        if (currentShift != null)
        {
            // Tải danh sách đơn hàng tiền mặt trong ca hiện tại
            viewModel.ShiftCashOrders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.PaymentTransactions)
                .Where(o => o.CashierShiftId == currentShift.ShiftId &&
                            o.PaymentTransactions.Any(p => p.PaymentMethod == "Cash" && p.PaymentStatus == "Success"))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenShift(CashierShiftViewModel model)
    {
        var cashierId = GetCurrentUserId();

        // Kiểm tra xem cashier đã có ca mở chưa
        var existingOpenShift = await _context.CashierShifts
            .AnyAsync(s => s.CashierId == cashierId && s.ShiftStatus == "Open");

        if (existingOpenShift)
        {
            TempData["ErrorMessage"] = "Bạn đang có một ca làm việc đang mở. Không thể mở thêm ca mới!";
            return RedirectToAction(nameof(Shift));
        }

        if (!model.IsOpeningCashConfirmed)
        {
            TempData["ErrorMessage"] = "Vui lòng tích chọn xác nhận đã kiểm đếm đúng số tiền mặt đầu ca!";
            return RedirectToAction(nameof(Shift));
        }

        if (model.InputOpeningCash < 0)
        {
            TempData["ErrorMessage"] = "Số tiền mặt đầu ca không thể là số âm!";
            return RedirectToAction(nameof(Shift));
        }

        // Tìm ca đóng gần nhất để liên kết PreviousShiftId
        var lastClosedShift = await _context.CashierShifts
            .Where(s => s.ShiftStatus == "Closed" || s.ShiftStatus == "HandedOver")
            .OrderByDescending(s => s.EndTime ?? s.StartTime)
            .FirstOrDefaultAsync();

        var newShift = new CashierShift
        {
            CashierId = cashierId,
            PreviousShiftId = lastClosedShift?.ShiftId,
            StartTime = DateTime.Now,
            OpeningCash = model.InputOpeningCash,
            IsOpeningCashConfirmed = true,
            SystemCashSales = 0,
            ShiftStatus = "Open",
            HandoverNotes = string.IsNullOrWhiteSpace(model.InputOpeningNotes) ? "Nhận ca đầu ngày" : model.InputOpeningNotes.Trim()
        };

        _context.CashierShifts.Add(newShift);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Mở ca làm việc #{newShift.ShiftId} thành công với số tiền đầu ca {newShift.OpeningCash:N0} đ!";
        return RedirectToAction(nameof(Shift));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseShift(CashierShiftViewModel model)
    {
        var cashierId = GetCurrentUserId();

        var currentShift = await _context.CashierShifts
            .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.ShiftStatus == "Open");

        if (currentShift == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy ca làm việc đang mở để đóng!";
            return RedirectToAction(nameof(Shift));
        }

        if (!model.ClosingActualCash.HasValue || model.ClosingActualCash.Value < 0)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập số tiền mặt thực tế trong két cuối ca hợp lệ (>= 0)!";
            return RedirectToAction(nameof(Shift));
        }

        currentShift.ClosingActualCash = model.ClosingActualCash.Value;
        currentShift.HandoverNotes = model.HandoverNotes?.Trim();
        currentShift.EndTime = DateTime.Now;
        currentShift.ShiftStatus = "Closed";

        // LƯU Ý QUAN TRỌNG: Discrepancy là Computed Column của SQL Server:
        // Discrepancy AS (ClosingActualCash - (OpeningCash + SystemCashSales))
        // Tuyệt đối KHÔNG gán currentShift.Discrepancy = ...

        await _context.SaveChangesAsync();

        // Nạp lại dữ liệu từ DB để nhận giá trị Discrepancy vừa được SQL Server tính toán
        await _context.Entry(currentShift).ReloadAsync();

        var diff = currentShift.Discrepancy ?? 0;
        string diffText;
        if (diff == 0)
        {
            diffText = "Tiền mặt khớp tuyệt đối (0 đ)";
        }
        else if (diff > 0)
        {
            diffText = $"Thừa {diff:N0} đ";
        }
        else
        {
            diffText = $"Thiếu {Math.Abs(diff):N0} đ";
        }

        TempData["SuccessMessage"] = $"Đóng ca #{currentShift.ShiftId} thành công! Tiền thực tế: {currentShift.ClosingActualCash:N0} đ. Chênh lệch: {diffText}.";
        return RedirectToAction(nameof(Shift));
    }

    // =========================================================================
    // 2. BÁN HÀNG TẠI QUẦY (POS)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> POS(int? categoryId = null, string? searchPhone = null, int pointsUsed = 0)
    {
        var cashierId = GetCurrentUserId();

        // Kiểm tra ca làm việc đang mở
        var currentShift = await _context.CashierShifts
            .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.ShiftStatus == "Open");

        if (currentShift == null)
        {
            TempData["ErrorMessage"] = "Bạn chưa mở ca làm việc! Vui lòng kiểm đếm tiền và mở ca trước khi vào màn hình POS.";
            return RedirectToAction(nameof(Shift));
        }

        // Tải Categories
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();

        // Tải Products kèm ProductVariants
        var prodQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants.Where(v => v.IsActive))
            .Where(p => p.IsAvailable);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            prodQuery = prodQuery.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await prodQuery.ToListAsync();

        // Xử lý thông tin khách hàng
        if (!string.IsNullOrWhiteSpace(searchPhone))
        {
            HttpContext.Session.SetString(PosCustomerPhoneKey, searchPhone.Trim());
        }

        var customerPhone = HttpContext.Session.GetString(PosCustomerPhoneKey);
        User? customer = null;
        if (!string.IsNullOrWhiteSpace(customerPhone))
        {
            customer = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Phone == customerPhone);
        }

        // Tải giỏ hàng từ Session
        var cart = GetCart();

        var viewModel = new PosViewModel
        {
            Categories = categories,
            Products = products,
            SelectedCategoryId = categoryId,
            SelectedCustomer = customer,
            SearchPhone = customerPhone,
            CartItems = cart,
            CurrentShift = currentShift,
            PointsUsed = pointsUsed
        };

        // Đảm bảo điểm dùng không vượt quá mức cho phép
        if (viewModel.PointsUsed > viewModel.MaxPointsUsable)
        {
            viewModel.PointsUsed = viewModel.MaxPointsUsable;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToPosCart(int variantId, string sugarLevel, string iceLevel, int quantity, string? itemNote, int? categoryId)
    {
        if (quantity <= 0) quantity = 1;

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.IsActive);

        if (variant == null)
        {
            TempData["ErrorMessage"] = "Biến thể sản phẩm không tồn tại hoặc đã ngừng phục vụ.";
            return RedirectToAction(nameof(POS), new { categoryId });
        }

        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.VariantId == variantId &&
                                               c.SugarLevel == sugarLevel &&
                                               c.IceLevel == iceLevel &&
                                               c.ItemNote == itemNote);

        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            cart.Add(new PosCartItem
            {
                VariantId = variant.VariantId,
                ProductId = variant.ProductId,
                ProductName = variant.Product.ProductName,
                SizeName = variant.SizeName,
                UnitPrice = variant.Price,
                Quantity = quantity,
                SugarLevel = string.IsNullOrWhiteSpace(sugarLevel) ? "100%" : sugarLevel,
                IceLevel = string.IsNullOrWhiteSpace(iceLevel) ? "100%" : iceLevel,
                ItemNote = itemNote
            });
        }

        SaveCart(cart);
        TempData["SuccessMessage"] = $"Đã thêm {quantity}x {variant.Product.ProductName} (Size {variant.SizeName}) vào đơn!";
        return RedirectToAction(nameof(POS), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromPosCart(int index, int? categoryId)
    {
        var cart = GetCart();
        if (index >= 0 && index < cart.Count)
        {
            var item = cart[index];
            cart.RemoveAt(index);
            SaveCart(cart);
            TempData["SuccessMessage"] = $"Đã xóa món {item.ProductName} ({item.SizeName}) khỏi đơn.";
        }

        return RedirectToAction(nameof(POS), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearPosCart(int? categoryId)
    {
        SaveCart(new List<PosCartItem>());
        TempData["SuccessMessage"] = "Đã làm trống giỏ hàng POS.";
        return RedirectToAction(nameof(POS), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchCustomer(string searchPhone, int? categoryId)
    {
        if (string.IsNullOrWhiteSpace(searchPhone))
        {
            HttpContext.Session.Remove(PosCustomerPhoneKey);
            TempData["WarningMessage"] = "Vui lòng nhập số điện thoại khách hàng.";
            return RedirectToAction(nameof(POS), new { categoryId });
        }

        var trimmedPhone = searchPhone.Trim();
        var customer = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == trimmedPhone);

        if (customer == null)
        {
            TempData["WarningMessage"] = $"Không tìm thấy khách hàng với SĐT {trimmedPhone}. Bạn có thể tạo nhanh khách hàng bên dưới.";
            HttpContext.Session.Remove(PosCustomerPhoneKey);
        }
        else
        {
            HttpContext.Session.SetString(PosCustomerPhoneKey, trimmedPhone);
            TempData["SuccessMessage"] = $"Đã chọn khách hàng: {customer.FullName} ({customer.LoyaltyPoints} điểm).";
        }

        return RedirectToAction(nameof(POS), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveCustomer(int? categoryId)
    {
        HttpContext.Session.Remove(PosCustomerPhoneKey);
        TempData["SuccessMessage"] = "Đã hủy chọn khách hàng (chuyển sang khách vãng lai).";
        return RedirectToAction(nameof(POS), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCustomer(string fullName, string phone, string? email, string? address, int? categoryId)
    {
        var trimmedPhone = phone?.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(trimmedPhone))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập Họ tên và Số điện thoại khách hàng.";
            return RedirectToAction(nameof(POS), new { categoryId });
        }

        if (await _context.Users.AnyAsync(u => u.Phone == trimmedPhone))
        {
            HttpContext.Session.SetString(PosCustomerPhoneKey, trimmedPhone);
            TempData["WarningMessage"] = $"SĐT {trimmedPhone} đã tồn tại trong hệ thống. Đã tự động chọn khách hàng này.";
            return RedirectToAction(nameof(POS), new { categoryId });
        }

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
        if (customerRole == null)
        {
            TempData["ErrorMessage"] = "Lỗi hệ thống: Không tìm thấy vai trò Customer.";
            return RedirectToAction(nameof(POS), new { categoryId });
        }

        var newCustomer = new User
        {
            RoleId = customerRole.RoleId,
            FullName = fullName.Trim(),
            Phone = trimmedPhone,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            PasswordHash = "123456",
            LoyaltyPoints = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(newCustomer);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString(PosCustomerPhoneKey, trimmedPhone);
        TempData["SuccessMessage"] = $"Tạo nhanh khách hàng {newCustomer.FullName} thành công!";
        return RedirectToAction(nameof(POS), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckoutPosOrder(int pointsUsed, string paymentMethod, string? orderNote)
    {
        var cashierId = GetCurrentUserId();

        var currentShift = await _context.CashierShifts
            .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.ShiftStatus == "Open");

        if (currentShift == null)
        {
            TempData["ErrorMessage"] = "Bạn chưa mở ca làm việc! Không thể thanh toán.";
            return RedirectToAction(nameof(Shift));
        }

        var cart = GetCart();
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng POS đang trống. Vui lòng thêm đồ uống vào đơn!";
            return RedirectToAction(nameof(POS));
        }

        var subTotal = cart.Sum(c => c.LineTotal);

        // Khách hàng
        var customerPhone = HttpContext.Session.GetString(PosCustomerPhoneKey);
        User? customer = null;
        if (!string.IsNullOrWhiteSpace(customerPhone))
        {
            customer = await _context.Users.FirstOrDefaultAsync(u => u.Phone == customerPhone);
        }

        // Tính điểm tích lũy dùng: 1 điểm = 1.000đ
        int maxPoints = customer != null ? Math.Min(customer.LoyaltyPoints, (int)(subTotal / 1000m)) : 0;
        if (pointsUsed < 0) pointsUsed = 0;
        if (pointsUsed > maxPoints) pointsUsed = maxPoints;

        decimal discountAmount = pointsUsed * 1000m;
        decimal totalAmount = Math.Max(0, subTotal - discountAmount);

        // Điểm thưởng mới tích lũy: TotalAmount / 10000
        int earnedPoints = (int)(totalAmount / 10000m);

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            paymentMethod = "Cash";
        }

        var orderCode = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");

        // 1. Tạo bản ghi Orders
        var order = new Order
        {
            OrderCode = orderCode,
            CustomerId = customer?.UserId,
            CashierId = cashierId,
            CashierShiftId = currentShift.ShiftId,
            BaristaId = null,
            OrderSource = "Counter",
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            ShippingFee = 0,
            TotalAmount = totalAmount,
            LoyaltyPointsUsed = pointsUsed,
            LoyaltyPointsEarned = earnedPoints,
            PreparationStatus = "Pending",
            OrderNote = orderNote?.Trim(),
            CreatedAt = DateTime.Now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // 2. Tạo OrderItems
        foreach (var item in cart)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                VariantId = item.VariantId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SugarLevel = item.SugarLevel,
                IceLevel = item.IceLevel,
                ItemNote = item.ItemNote
            };
            _context.OrderItems.Add(orderItem);
        }

        // 3. Tạo bản ghi PaymentTransactions (PaymentStatus = "Success")
        var payment = new PaymentTransaction
        {
            OrderId = order.OrderId,
            PaymentMethod = paymentMethod,
            TransactionType = "Payment",
            Amount = totalAmount,
            PaymentStatus = "Success",
            GatewayTransactionRef = paymentMethod == "Cash" ? "POS-CASH" : "POS-TRANSFER-" + order.OrderId,
            CreatedAt = DateTime.Now
        };
        _context.PaymentTransactions.Add(payment);

        // 4. Nếu thanh toán bằng "Cash", cộng TotalAmount vào CashierShifts.SystemCashSales
        if (paymentMethod == "Cash")
        {
            currentShift.SystemCashSales += totalAmount;
        }

        // 5. Cập nhật LoyaltyPoints cho Khách hàng
        if (customer != null)
        {
            customer.LoyaltyPoints = customer.LoyaltyPoints - pointsUsed + earnedPoints;
        }

        await _context.SaveChangesAsync();

        // 6. Dọn dẹp giỏ hàng POS và khách hàng trong Session
        SaveCart(new List<PosCartItem>());
        HttpContext.Session.Remove(PosCustomerPhoneKey);

        TempData["SuccessMessage"] = $"Tạo đơn #{order.OrderCode} thành công! Thanh toán: {(paymentMethod == "Cash" ? "Tiền mặt" : "Chuyển khoản")}. Tổng tiền: {totalAmount:N0} đ.";
        return RedirectToAction(nameof(Bill), new { id = order.OrderId });
    }

    // =========================================================================
    // 3. HÓA ĐƠN BÁN HÀNG TẠI QUẦY (Bill - GET)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> Bill(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Cashier)
            .Include(o => o.PaymentTransactions)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // =========================================================================
    // PRIVATE HELPERS (Session Cart & Current User)
    // =========================================================================

    private List<PosCartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(PosCartSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new List<PosCartItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<PosCartItem>>(json) ?? new List<PosCartItem>();
        }
        catch
        {
            return new List<PosCartItem>();
        }
    }

    private void SaveCart(List<PosCartItem> cart)
    {
        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(PosCartSessionKey, json);
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int id))
        {
            return id;
        }

        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        if (sessionUserId.HasValue)
        {
            return sessionUserId.Value;
        }

        return 2; // Fallback demo CashierId
    }
}
