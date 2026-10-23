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
        var deliveries = await _context.Deliveries
            .Include(d => d.Order)
            .OrderByDescending(d => d.AssignedAt)
            .ToListAsync();

        return View(deliveries);
    }
}
