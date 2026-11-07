using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;

namespace CafeManagementSystem.Controllers;

[AllowAnonymous]
public class LandingController : Controller
{
    private readonly CafeManagementDbContext _context;

    public LandingController(CafeManagementDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new LandingPageViewModel();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var dbCategories = await _context.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryId)
                .ToListAsync(cts.Token);

            var dbProducts = await _context.Products.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.CategoryId)
                .ThenBy(p => p.ProductId)
                .ToListAsync(cts.Token);

            // 1. Đồng bộ danh mục từ database
            model.Categories = dbCategories.Select(c => new LandingCategoryItem
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ProductCount = dbProducts.Count(p => p.CategoryId == c.CategoryId)
            }).ToList();

            // 2. Đồng bộ toàn bộ sản phẩm đang bán từ database
            foreach (var p in dbProducts)
            {
                var activeVariants = p.ProductVariants.Where(v => v.IsActive).ToList();
                var minPrice = activeVariants.Any() ? activeVariants.Min(v => v.Price) : 0;

                var item = new LandingFeaturedProductItem
                {
                    ProductId = p.ProductId,
                    CategoryId = p.CategoryId,
                    ProductName = p.ProductName,
                    CategoryName = p.Category?.CategoryName ?? "Đồ uống",
                    MinPrice = minPrice,
                    ImageUrl = !string.IsNullOrWhiteSpace(p.ImageUrl) ? p.ImageUrl : "/images/products/default.jpg",
                    Description = p.Description
                };

                model.AllProducts.Add(item);
            }

            // 3. 3 món đặc trưng tiêu biểu nhất theo sát ảnh tham chiếu (Bạc xỉu, Cà phê sữa đá, Trà đào cam sả)
            var bacXiu = model.AllProducts.FirstOrDefault(p => 
                p.ProductName.Contains("Bạc Xỉu", StringComparison.OrdinalIgnoreCase) ||
                p.ProductName.Contains("Bac Xiu", StringComparison.OrdinalIgnoreCase));

            var cfSua = model.AllProducts.FirstOrDefault(p => 
                p.ProductName.Contains("Sữa Đá", StringComparison.OrdinalIgnoreCase) ||
                p.ProductName.Contains("Sua Da", StringComparison.OrdinalIgnoreCase));

            var traDao = model.AllProducts.FirstOrDefault(p => 
                p.ProductName.Contains("Đào", StringComparison.OrdinalIgnoreCase) ||
                p.ProductName.Contains("Dao", StringComparison.OrdinalIgnoreCase));

            if (bacXiu != null) model.FeaturedProducts.Add(bacXiu);
            if (cfSua != null) model.FeaturedProducts.Add(cfSua);
            if (traDao != null) model.FeaturedProducts.Add(traDao);

            // Bổ sung nếu thiếu để luôn đủ 3 món hàng đầu
            foreach (var p in model.AllProducts)
            {
                if (model.FeaturedProducts.Count >= 3) break;
                if (!model.FeaturedProducts.Contains(p))
                {
                    model.FeaturedProducts.Add(p);
                }
            }
        }
        catch
        {
            // Dự phòng an toàn nếu database chưa sẵn sàng
            model.FeaturedProducts = new List<LandingFeaturedProductItem>
            {
                new() { ProductId = 1, CategoryId = 1, ProductName = "Bạc Xỉu Đá", CategoryName = "Cà phê Việt Nam", MinPrice = 29000, ImageUrl = "/images/products/bac-xiu.jpg" },
                new() { ProductId = 2, CategoryId = 1, ProductName = "Cà Phê Sữa Đá", CategoryName = "Cà phê Việt Nam", MinPrice = 29000, ImageUrl = "/images/products/cf-sua.jpg" },
                new() { ProductId = 3, CategoryId = 2, ProductName = "Trà Đào Cam Sả", CategoryName = "Trà trái cây", MinPrice = 42000, ImageUrl = "/images/products/tra-dao.jpg" }
            };
            model.AllProducts = new List<LandingFeaturedProductItem>(model.FeaturedProducts);
        }

        return View(model);
    }
}
