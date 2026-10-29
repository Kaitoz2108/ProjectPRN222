using System.Data;
using System.Security.Claims;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagementSystem.Controllers;

public partial class AdminController
{
    // Prefer this explicit route to the existing conventional Dashboard action.
    [HttpGet("/Admin/Dashboard", Order = -1)]
    [ActionName("Dashboard")]
    public async Task<IActionResult> OperationsDashboard(DateTime? fromDate, DateTime? toDate)
    {
        var from = (fromDate ?? DateTime.Today.AddDays(-29)).Date;
        var to = (toDate ?? DateTime.Today).Date;
        if (from > to || to == DateTime.MaxValue.Date)
            ModelState.AddModelError("", "Khoảng ngày không hợp lệ: ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        var model = new AdminDashboardViewModel { FromDate = from, ToDate = to };
        if (!ModelState.IsValid) return View("Dashboard", model);
        var end = to.AddDays(1);
        var orders = _context.Orders.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.TotalOrders = await orders.CountAsync();
        model.OnlineOrders = await orders.CountAsync(x => x.OrderSource == "Online");
        model.CounterOrders = await orders.CountAsync(x => x.OrderSource == "Counter");
        // Cash basis: receipts and refunds by transaction date, including older orders.
        var payments = _context.PaymentTransactions.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.GrossRevenue = await payments.Where(x => x.TransactionType == "Payment" && x.PaymentStatus == "Success").SumAsync(x => (decimal?)x.Amount) ?? 0;
        model.RefundAmount = await payments.Where(x => x.TransactionType == "Refund" && x.PaymentStatus == "Refunded").SumAsync(x => (decimal?)x.Amount) ?? 0;
        model.TopProducts = await _context.OrderItems.AsNoTracking()
            .Where(x => x.Order.CreatedAt >= from && x.Order.CreatedAt < end && x.Order.PreparationStatus != "Cancelled"
                && x.Order.PaymentTransactions.Any(p => p.TransactionType == "Payment" && p.PaymentStatus == "Success"))
            .GroupBy(x => new { x.Variant.ProductId, x.Variant.Product.ProductName })
            .Select(g => new AdminProductSalesRow { ProductName = g.Key.ProductName, Quantity = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Quantity).ThenBy(x => x.ProductName).Take(5).ToListAsync();
        model.CashierShifts = await _context.CashierShifts.AsNoTracking().Include(x => x.Cashier)
            .Where(x => x.StartTime >= from && x.StartTime < end).OrderByDescending(x => x.StartTime).ToListAsync();
        var inventory = _context.InventoryTransactions.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.RestockCost = await inventory.Where(x => x.TransactionType == "Restock")
            .SumAsync(x => (decimal?)(Math.Abs(x.QuantityChanged) * (x.UnitCost ?? 0))) ?? 0;
        // Keep quantities separated by ingredient/unit.
        model.Inventory = await inventory.Where(x => x.TransactionType == "OrderDeduction" || x.TransactionType == "Wastage")
            .GroupBy(x => new { x.IngredientId, x.Ingredient.IngredientName, x.Ingredient.Unit })
            .Select(g => new AdminInventoryRow {
                IngredientName = g.Key.IngredientName, Unit = g.Key.Unit,
                OrderDeduction = g.Sum(x => x.TransactionType == "OrderDeduction" ? Math.Abs(x.QuantityChanged) : 0),
                Wastage = g.Sum(x => x.TransactionType == "Wastage" ? Math.Abs(x.QuantityChanged) : 0)
            }).OrderBy(x => x.IngredientName).ToListAsync();
        var complaints = _context.Complaints.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.TotalComplaints = await complaints.CountAsync();
        model.PendingComplaints = await complaints.CountAsync(x => x.ComplaintStatus == "Pending");
        model.AverageRating = await complaints.AverageAsync(x => (double?)x.Rating);
        return View("Dashboard", model);
    }
}
