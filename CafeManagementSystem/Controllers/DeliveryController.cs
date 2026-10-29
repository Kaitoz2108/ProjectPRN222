using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Controllers;

[Authorize(Roles = "DeliveryStaff,Admin")]
public class DeliveryController : Controller
{
    private readonly CafeManagementDbContext _context;

    public DeliveryController(CafeManagementDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Delivery)
            .Include(o => o.PaymentTransactions)
            .Include(o => o.OrderItems)
                .ThenInclude(item => item.Variant)
                    .ThenInclude(variant => variant.Product)
            .Where(o => o.OrderSource == "Online" &&
                ((o.Delivery == null && o.PreparationStatus == "Ready") ||
                 (o.Delivery != null && o.Delivery.DeliveryStatus != "Delivered")))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptOrder(int orderId, string recipientName, string recipientPhone, string shippingAddress)
    {
        if (!TryGetCurrentUserId(out var deliveryStaffId))
        {
            return Forbid();
        }

        recipientName = recipientName?.Trim() ?? string.Empty;
        recipientPhone = recipientPhone?.Trim() ?? string.Empty;
        shippingAddress = shippingAddress?.Trim() ?? string.Empty;

        if (recipientName.Length is 0 or > 100 || recipientPhone.Length is 0 or > 20 || shippingAddress.Length is 0 or > 300)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập tên, số điện thoại và địa chỉ giao hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var order = await _context.Orders
            .Include(o => o.Delivery)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null || order.OrderSource != "Online" || order.PreparationStatus != "Ready")
        {
            TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc chưa sẵn sàng giao.";
            return RedirectToAction(nameof(Index));
        }

        if (order.Delivery != null)
        {
            TempData["ErrorMessage"] = "Đơn hàng này đã được Shipper khác nhận.";
            return RedirectToAction(nameof(Index));
        }

        var successfulPayments = order.PaymentTransactions
            .Where(payment => payment.PaymentStatus == "Success")
            .Sum(payment => payment.Amount);

        var delivery = new Delivery
        {
            OrderId = order.OrderId,
            DeliveryStaffId = deliveryStaffId,
            RecipientName = recipientName,
            RecipientPhone = recipientPhone,
            ShippingAddress = shippingAddress,
            DeliveryStatus = "Assigned",
            CodAmount = Math.Max(0m, order.TotalAmount - successfulPayments),
            IsCodCollected = false,
            AssignedAt = DateTime.Now
        };

        _context.Deliveries.Add(delivery);
        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã nhận giao đơn #{order.OrderCode}.";
        }
        catch (DbUpdateException)
        {
            _context.Entry(delivery).State = EntityState.Detached;
            TempData["ErrorMessage"] = "Đơn hàng vừa được Shipper khác nhận. Vui lòng tải lại danh sách.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeliveryStatus(int deliveryId, string newStatus, bool isCodCollected, string? problemNote)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Forbid();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.DeliveryId == deliveryId);

        if (delivery == null || delivery.Order.OrderSource != "Online" ||
            (!User.IsInRole("Admin") && delivery.DeliveryStaffId != currentUserId))
        {
            await transaction.RollbackAsync();
            return NotFound();
        }

        var isValidTransition = (delivery.DeliveryStatus, newStatus) switch
        {
            ("Assigned", "PickedUp") => true,
            ("PickedUp", "Delivering") => true,
            ("Delivering", "Delivered") => true,
            ("Delivering", "Failed") => true,
            _ => false
        };

        if (!isValidTransition)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Không thể chuyển sang trạng thái giao hàng này.";
            return RedirectToAction(nameof(Index));
        }

        problemNote = problemNote?.Trim();
        if (newStatus == "Failed" && (string.IsNullOrWhiteSpace(problemNote) || problemNote.Length > 500))
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Vui lòng nhập mô tả sự cố (tối đa 500 ký tự).";
            return RedirectToAction(nameof(Index));
        }

        if (newStatus == "Delivered" && delivery.CodAmount > 0 && !isCodCollected)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Đơn có COD; vui lòng xác nhận đã thu tiền mặt trước khi hoàn tất.";
            return RedirectToAction(nameof(Index));
        }

        if (newStatus == "Delivered" && delivery.CodAmount > 0)
        {
            var openShift = await _context.CashierShifts
                .Where(shift => shift.ShiftStatus == "Open")
                .OrderByDescending(shift => shift.StartTime)
                .FirstOrDefaultAsync();

            if (openShift == null)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không tìm thấy ca thu ngân đang mở để bàn giao tiền COD.";
                return RedirectToAction(nameof(Index));
            }

            delivery.IsCodCollected = true;
            delivery.HandedCodToShiftId = openShift.ShiftId;
            openShift.SystemCashSales += delivery.CodAmount;
        }

        delivery.DeliveryStatus = newStatus;
        delivery.ProblemNote = newStatus == "Failed" ? problemNote : delivery.ProblemNote;
        if (newStatus == "Delivered")
        {
            delivery.DeliveredAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = newStatus == "Failed"
            ? $"Đã ghi nhận sự cố giao đơn #{delivery.Order.OrderCode}."
            : $"Đã cập nhật trạng thái đơn #{delivery.Order.OrderCode} thành {newStatus}.";
        return RedirectToAction(nameof(Index));
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
