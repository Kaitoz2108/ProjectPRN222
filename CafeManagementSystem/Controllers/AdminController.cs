using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public partial class AdminController : Controller
{
    protected readonly CafeManagementDbContext _context;

    public AdminController(CafeManagementDbContext context)
    {
        _context = context;
    }

    // Dashboard chính của Quản trị viên
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.TotalOrders = await _context.Orders.CountAsync();
        ViewBag.TotalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.LowStockIngredients = await _context.Ingredients
            .Where(i => i.StockQuantity <= i.MinStockLevel)
            .CountAsync();

        ViewBag.RecentOrders = await _context.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View();
    }
}
