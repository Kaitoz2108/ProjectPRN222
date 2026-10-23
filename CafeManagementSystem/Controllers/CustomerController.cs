using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Controllers;

[Authorize(Roles = "Customer,Admin")]
public class CustomerController : Controller
{
    private readonly CafeManagementDbContext _context;

    public CustomerController(CafeManagementDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Menu(int? categoryId = null)
    {
        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsActive)
            .ToListAsync();

        ViewBag.SelectedCategoryId = categoryId;

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants.Where(v => v.IsActive))
            .Where(p => p.IsAvailable);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query.ToListAsync();
        return View(products);
    }
}
