using System.Security.Claims;
using CafeManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagementSystem.Controllers;

[Authorize]
public class PaymentGatewayController : Controller
{
    private readonly CafeManagementDbContext _context;

    public PaymentGatewayController(CafeManagementDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Pay(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần thanh toán.";
            return RedirectToAction("Menu", "Customer");
        }

        var isPaid = order.PaymentTransactions.Any(p => p.TransactionType == "Payment" && p.PaymentStatus == "Success");
        if (isPaid)
        {
            TempData["SuccessMessage"] = "Đơn hàng này đã được thanh toán thành công!";
            return RedirectToAction("Bill", "Customer", new { id = order.OrderId });
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction("Menu", "Customer");
        }

        var payment = order.PaymentTransactions.FirstOrDefault(p => p.TransactionType == "Payment");
        if (payment == null)
        {
            payment = new PaymentTransaction
            {
                OrderId = order.OrderId,
                PaymentMethod = "Transfer",
                TransactionType = "Payment",
                Amount = order.TotalAmount,
                CreatedAt = DateTime.Now
            };
            _context.PaymentTransactions.Add(payment);
        }

        payment.PaymentStatus = "Success";
        payment.GatewayTransactionRef = $"GW-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        payment.GatewayResponseCode = "00"; // Giao dịch thành công

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Thanh toán thành công {order.TotalAmount:N0} đ qua cổng thanh toán mô phỏng!";
        return RedirectToAction("Bill", "Customer", new { id = order.OrderId });
    }
}

