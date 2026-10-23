using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Controllers;

[Authorize(Roles = "Barista,Admin")]
public class BaristaController : Controller
{
    private readonly CafeManagementDbContext _context;

    public BaristaController(CafeManagementDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> KDS()
    {
        var pendingOrders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.Customer)
            .Where(o => o.PreparationStatus == "Pending" || o.PreparationStatus == "Preparing")
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return View(pendingOrders);
    }
}
