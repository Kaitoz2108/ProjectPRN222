using System.Security.Claims;
using System.Text.Json;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagementSystem.Controllers;

[Authorize(Roles = "Customer,Admin")]
public class CustomerController : Controller
{
    private readonly CafeManagementDbContext _context;
    private const string CustomerCartSessionKey = "CustomerCart_Items";

    public CustomerController(CafeManagementDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(idStr, out int id);
        return id;
    }

    private List<CustomerCartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CustomerCartSessionKey);
        if (string.IsNullOrEmpty(json)) return new List<CustomerCartItem>();
        try
        {
            return JsonSerializer.Deserialize<List<CustomerCartItem>>(json) ?? new List<CustomerCartItem>();
        }
        catch
        {
            return new List<CustomerCartItem>();
        }
    }

    private void SaveCart(List<CustomerCartItem> cart)
    {
        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CustomerCartSessionKey, json);
    }

    // =========================================================================
    // 1. MENU (GET)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Menu(int? categoryId = null)
    {
        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsActive)
            .ToListAsync();

        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.CartCount = GetCart().Sum(c => c.Quantity);

        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            var u = await _context.Users.FindAsync(userId);
            ViewBag.UserPoints = u?.LoyaltyPoints ?? 0;
            ViewBag.UserName = u?.FullName ?? "Khách hàng";
        }
        else
        {
            ViewBag.UserPoints = 0;
            ViewBag.UserName = "Khách hàng";
        }

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants.Where(v => v.IsActive))
            .Where(p => p.IsAvailable && p.Category.IsActive);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query.ToListAsync();
        return View(products);
    }

    // =========================================================================
    // 2. GIỎ HÀNG (ADD, VIEW, UPDATE, REMOVE, CLEAR)
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int variantId, int quantity, string sugarLevel, string iceLevel, string? itemNote)
    {
        if (quantity <= 0) quantity = 1;

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.IsActive && v.Product.IsAvailable && v.Product.Category.IsActive);

        if (variant == null)
        {
            TempData["ErrorMessage"] = "Đồ uống hoặc kích cỡ này hiện không còn phục vụ.";
            return RedirectToAction(nameof(Menu));
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
            cart.Add(new CustomerCartItem
            {
                VariantId = variant.VariantId,
                ProductId = variant.ProductId,
                ProductName = variant.Product.ProductName,
                ImageUrl = variant.Product.ImageUrl,
                SizeName = variant.SizeName,
                UnitPrice = variant.Price,
                Quantity = quantity,
                SugarLevel = string.IsNullOrWhiteSpace(sugarLevel) ? "100%" : sugarLevel,
                IceLevel = string.IsNullOrWhiteSpace(iceLevel) ? "100%" : iceLevel,
                ItemNote = itemNote
            });
        }

        SaveCart(cart);
        TempData["SuccessMessage"] = $"Đã thêm {quantity}x {variant.Product.ProductName} (Size {variant.SizeName}) vào giỏ hàng!";
        return RedirectToAction(nameof(Menu));
    }

    [HttpGet]
    public async Task<IActionResult> Cart()
    {
        var cart = GetCart();
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        var model = new CustomerCheckoutViewModel
        {
            CartItems = cart,
            AvailableLoyaltyPoints = user?.LoyaltyPoints ?? 0,
            RecipientName = user?.FullName ?? string.Empty,
            RecipientPhone = user?.Phone ?? string.Empty,
            ShippingAddress = user?.Address ?? string.Empty,
            PaymentMethod = "COD"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCart(int variantId, int quantity, string sugarLevel, string iceLevel)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.VariantId == variantId && c.SugarLevel == sugarLevel && c.IceLevel == iceLevel);
        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            SaveCart(cart);
        }
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(int index)
    {
        var cart = GetCart();
        if (index >= 0 && index < cart.Count)
        {
            cart.RemoveAt(index);
            SaveCart(cart);
            TempData["SuccessMessage"] = "Đã xóa món khỏi giỏ hàng.";
        }
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCart()
    {
        SaveCart(new List<CustomerCartItem>());
        TempData["SuccessMessage"] = "Đã làm trống giỏ hàng.";
        return RedirectToAction(nameof(Cart));
    }

    // =========================================================================
    // 3. CHECKOUT ONLINE (POST)
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CustomerCheckoutViewModel model)
    {
        var cart = GetCart();
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng trực tuyến của bạn đang trống.";
            return RedirectToAction(nameof(Menu));
        }

        var userId = GetCurrentUserId();
        var customer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (customer == null)
        {
            return Challenge();
        }

        // Kiểm tra hợp lệ các món trong giỏ
        var variantIds = cart.Select(c => c.VariantId).Distinct().ToList();
        var validVariants = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
            .Where(v => variantIds.Contains(v.VariantId) && v.IsActive && v.Product.IsAvailable && v.Product.Category.IsActive)
            .ToDictionaryAsync(v => v.VariantId);

        if (validVariants.Count != variantIds.Count)
        {
            TempData["ErrorMessage"] = "Một số món trong giỏ hàng hiện đã ngừng phục vụ. Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Cart));
        }

        decimal subTotal = 0;
        foreach (var item in cart)
        {
            var v = validVariants[item.VariantId];
            item.UnitPrice = v.Price;
            item.ProductName = v.Product.ProductName;
            item.SizeName = v.SizeName;
            subTotal += item.UnitPrice * item.Quantity;
        }

        // Tính điểm sử dụng
        int maxPoints = Math.Min(customer.LoyaltyPoints, (int)(subTotal / 1000m));
        int pointsUsed = Math.Clamp(model.PointsUsed, 0, maxPoints);
        decimal discountAmount = pointsUsed * 1000m;
        decimal shippingFee = 15000m;
        decimal totalAmount = Math.Max(0, subTotal - discountAmount + shippingFee);
        int earnedPoints = (int)(totalAmount / 10000m);

        var paymentMethod = model.PaymentMethod == "Transfer" ? "Transfer" : "COD";
        var orderCode = $"ORD{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                OrderCode = orderCode,
                CustomerId = customer.UserId,
                CashierId = null,
                CashierShiftId = null,
                BaristaId = null,
                OrderSource = "Online",
                SubTotal = subTotal,
                DiscountAmount = discountAmount,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                LoyaltyPointsUsed = pointsUsed,
                LoyaltyPointsEarned = earnedPoints,
                PreparationStatus = "Pending",
                OrderNote = model.OrderNote?.Trim(),
                CreatedAt = DateTime.Now
            };

            foreach (var item in cart)
            {
                var note = item.ItemNote?.Trim();
                if (note?.Length > 255) note = note[..255];

                order.OrderItems.Add(new OrderItem
                {
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SugarLevel = item.SugarLevel,
                    IceLevel = item.IceLevel,
                    ItemNote = note
                });
            }

            // Gán Shipper (tìm shipper đang hoạt động, fallback đến User ID = 4 theo seed)
            var shipper = await _context.Users
                .FirstOrDefaultAsync(u => u.Role.RoleName == "DeliveryStaff" && u.IsActive);
            int shipperId = shipper?.UserId ?? 4;

            var delivery = new Delivery
            {
                Order = order,
                DeliveryStaffId = shipperId,
                RecipientName = model.RecipientName.Trim(),
                RecipientPhone = model.RecipientPhone.Trim(),
                ShippingAddress = model.ShippingAddress.Trim(),
                DeliveryStatus = "Assigned",
                CodAmount = paymentMethod == "COD" ? totalAmount : 0,
                IsCodCollected = false,
                AssignedAt = DateTime.Now
            };

            var payment = new PaymentTransaction
            {
                Order = order,
                PaymentMethod = paymentMethod,
                TransactionType = "Payment",
                Amount = totalAmount,
                PaymentStatus = "Pending",
                GatewayTransactionRef = paymentMethod == "COD" ? "COD-PENDING" : $"GW-ONLINE-{orderCode}",
                CreatedAt = DateTime.Now
            };

            // Cập nhật điểm tích lũy khách hàng
            customer.LoyaltyPoints = customer.LoyaltyPoints - pointsUsed + earnedPoints;

            _context.Orders.Add(order);
            _context.Deliveries.Add(delivery);
            _context.PaymentTransactions.Add(payment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Xóa giỏ hàng Session
            SaveCart(new List<CustomerCartItem>());

            TempData["SuccessMessage"] = $"Đặt đơn #{order.OrderCode} thành công!";

            if (paymentMethod == "Transfer")
            {
                return RedirectToAction("Pay", "PaymentGateway", new { orderId = order.OrderId });
            }

            return RedirectToAction(nameof(Bill), new { id = order.OrderId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Có lỗi xảy ra khi tạo đơn hàng online: {ex.Message}";
            return RedirectToAction(nameof(Cart));
        }
    }

    // =========================================================================
    // 4. HÓA ĐƠN ĐIỆN TỬ (GET)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Bill(int id)
    {
        var userId = GetCurrentUserId();
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Delivery!)
                .ThenInclude(d => d.DeliveryStaff)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
            return RedirectToAction(nameof(Menu));
        }

        // Customer chỉ xem đơn của chính mình (Admin xem được tất cả)
        if (!User.IsInRole("Admin") && order.CustomerId != userId)
        {
            return Forbid();
        }

        return View(order);
    }

    // =========================================================================
    // 5. LỊCH SỬ ĐƠN HÀNG (GET)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var userId = GetCurrentUserId();
        var orders = await _context.Orders
            .Include(o => o.Delivery!)
                .ThenInclude(d => d.DeliveryStaff)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.PaymentTransactions)
            .Include(o => o.Complaints)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(new MyOrdersViewModel { Orders = orders });
    }

    // =========================================================================
    // 6. KHIẾU NẠI & PHẢN HỒI (GET & POST)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Complaints()
    {
        var userId = GetCurrentUserId();

        var eligibleOrders = await _context.Orders
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var complaints = await _context.Complaints
            .Include(c => c.Order)
            .Include(c => c.ResolvedByAdmin)
            .Where(c => c.CustomerId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var viewModel = new CustomerComplaintViewModel
        {
            EligibleOrders = eligibleOrders,
            Complaints = complaints
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitComplaint(CustomerComplaintInputModel model)
    {
        var userId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu phản hồi/khiếu nại không hợp lệ.";
            return RedirectToAction(nameof(Complaints));
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.CustomerId == userId);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Đơn hàng không thuộc tài khoản của bạn.";
            return RedirectToAction(nameof(Complaints));
        }

        var complaint = new Complaint
        {
            OrderId = order.OrderId,
            CustomerId = userId,
            ComplaintType = model.ComplaintType,
            Rating = model.Rating,
            Content = model.Content.Trim(),
            EvidenceImageUrl = string.IsNullOrWhiteSpace(model.EvidenceImageUrl) ? null : model.EvidenceImageUrl.Trim(),
            ComplaintStatus = "Pending",
            IsRefundApproved = false,
            CreatedAt = DateTime.Now
        };

        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã gửi phản hồi/khiếu nại cho đơn #{order.OrderCode}. Ban quản trị sẽ xử lý sớm nhất!";
        return RedirectToAction(nameof(Complaints));
    }
}
