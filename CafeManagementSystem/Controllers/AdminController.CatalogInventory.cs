using System.Security.Claims;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagementSystem.Controllers;

public partial class AdminController
{
    private int GetAdminUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(idStr, out int id);
        return id > 0 ? id : 1;
    }

    // =========================================================================
    // 1. QUẢN LÝ THỰC ĐƠN, SẢN PHẨM & CÔNG THỨC (PRODUCTS & RECIPES)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Products(int? categoryId)
    {
        var catQuery = _context.Categories.AsQueryable();
        var prodQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.ProductRecipes)
                    .ThenInclude(pr => pr.Ingredient)
            .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            prodQuery = prodQuery.Where(p => p.CategoryId == categoryId.Value);
        }

        var model = new AdminProductsViewModel
        {
            Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(),
            Products = await prodQuery.OrderBy(p => p.CategoryId).ThenBy(p => p.ProductName).ToListAsync(),
            Ingredients = await _context.Ingredients.OrderBy(i => i.IngredientName).ToListAsync(),
            SelectedCategoryId = categoryId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(AdminCreateProductInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu tạo sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Products), new { categoryId = input.CategoryId });
        }

        var product = new Product
        {
            CategoryId = input.CategoryId,
            ProductName = input.ProductName.Trim(),
            Description = input.Description?.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? "/images/products/default.jpg" : input.ImageUrl.Trim(),
            IsAvailable = input.IsAvailable
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Tạo sẵn biến thể mặc định Size M
        _context.ProductVariants.Add(new ProductVariant
        {
            ProductId = product.ProductId,
            SizeName = "M",
            Price = 30000m,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã tạo mới sản phẩm '{product.ProductName}' kèm Size M mặc định.";
        return RedirectToAction(nameof(Products), new { categoryId = input.CategoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductStatus(int productId, int? categoryId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Products), new { categoryId });
        }

        product.IsAvailable = !product.IsAvailable;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã {(product.IsAvailable ? "kích hoạt" : "tạm ngừng kinh doanh")} món '{product.ProductName}'.";
        return RedirectToAction(nameof(Products), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveVariant(AdminSaveVariantInput input, int? categoryId)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin biến thể/kích cỡ không hợp lệ.";
            return RedirectToAction(nameof(Products), new { categoryId });
        }

        if (input.VariantId.HasValue && input.VariantId.Value > 0)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == input.VariantId.Value);
            if (variant == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kích cỡ cần sửa.";
                return RedirectToAction(nameof(Products), new { categoryId });
            }
            variant.SizeName = input.SizeName.Trim();
            variant.Price = input.Price;
            variant.IsActive = input.IsActive;
        }
        else
        {
            _context.ProductVariants.Add(new ProductVariant
            {
                ProductId = input.ProductId,
                SizeName = input.SizeName.Trim(),
                Price = input.Price,
                IsActive = input.IsActive
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã lưu thông tin kích cỡ và giá bán thành công.";
        return RedirectToAction(nameof(Products), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRecipe(AdminSaveRecipeInput input, int? categoryId)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu công thức pha chế không hợp lệ.";
            return RedirectToAction(nameof(Products), new { categoryId });
        }

        var existing = await _context.ProductRecipes
            .FirstOrDefaultAsync(r => r.VariantId == input.VariantId && r.IngredientId == input.IngredientId);

        if (existing != null)
        {
            existing.QuantityRequired = input.QuantityRequired;
            existing.PreparationGuide = input.PreparationGuide?.Trim();
        }
        else
        {
            _context.ProductRecipes.Add(new ProductRecipe
            {
                VariantId = input.VariantId,
                IngredientId = input.IngredientId,
                QuantityRequired = input.QuantityRequired,
                PreparationGuide = input.PreparationGuide?.Trim()
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã lưu công thức định lượng nguyên liệu cho món.";
        return RedirectToAction(nameof(Products), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRecipe(int recipeId, int? categoryId)
    {
        var recipe = await _context.ProductRecipes.FirstOrDefaultAsync(r => r.RecipeId == recipeId);
        if (recipe != null)
        {
            _context.ProductRecipes.Remove(recipe);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa thành phần khỏi công thức.";
        }
        return RedirectToAction(nameof(Products), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string categoryName, string? description)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            TempData["ErrorMessage"] = "Tên danh mục không được để trống.";
            return RedirectToAction(nameof(Products));
        }

        var cat = new Category
        {
            CategoryName = categoryName.Trim(),
            Description = description?.Trim(),
            IsActive = true
        };

        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã tạo mới danh mục '{cat.CategoryName}'.";
        return RedirectToAction(nameof(Products), new { categoryId = cat.CategoryId });
    }

    // =========================================================================
    // 2. QUẢN LÝ KHO & NHẬP KHO (INVENTORY & RESTOCK)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Inventory()
    {
        var ingredients = await _context.Ingredients
            .OrderBy(i => i.StockQuantity <= i.MinStockLevel ? 0 : 1)
            .ThenBy(i => i.IngredientName)
            .ToListAsync();

        var recentTransactions = await _context.InventoryTransactions
            .Include(t => t.Ingredient)
            .Include(t => t.PerformedByUser)
            .OrderByDescending(t => t.CreatedAt)
            .Take(30)
            .ToListAsync();

        var model = new AdminInventoryViewModel
        {
            Ingredients = ingredients,
            RecentTransactions = recentTransactions
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restock(AdminRestockInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu nhập kho không hợp lệ.";
            return RedirectToAction(nameof(Inventory));
        }

        var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngredientId == input.IngredientId);
        if (ingredient == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nguyên liệu cần nhập kho.";
            return RedirectToAction(nameof(Inventory));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Tăng số lượng tồn kho
            ingredient.StockQuantity += input.Quantity;
            ingredient.LastUpdated = DateTime.Now;

            var adminId = GetAdminUserId();

            // Ghi nhận biến động kho Restock kèm UnitCost (để Dashboard tính RestockCost)
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                IngredientId = ingredient.IngredientId,
                TransactionType = "Restock",
                QuantityChanged = input.Quantity,
                UnitCost = input.UnitCost,
                PerformedByUserId = adminId,
                ReasonOrNote = string.IsNullOrWhiteSpace(input.ReasonOrNote) ? "Nhập bổ sung kho định kỳ" : input.ReasonOrNote.Trim(),
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var totalCost = input.Quantity * input.UnitCost;
            TempData["SuccessMessage"] = $"Nhập kho thành công {input.Quantity:N2} {ingredient.Unit} {ingredient.IngredientName}. Chi phí: {totalCost:N0} đ.";
            return RedirectToAction(nameof(Inventory));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Có lỗi xảy ra khi nhập kho: {ex.Message}";
            return RedirectToAction(nameof(Inventory));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIngredient(AdminCreateIngredientInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin nguyên liệu không hợp lệ.";
            return RedirectToAction(nameof(Inventory));
        }

        var ingredient = new Ingredient
        {
            IngredientName = input.IngredientName.Trim(),
            Unit = input.Unit.Trim(),
            StockQuantity = input.InitialStock,
            MinStockLevel = input.MinStockLevel,
            LastUpdated = DateTime.Now
        };

        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        if (input.InitialStock > 0)
        {
            var adminId = GetAdminUserId();
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                IngredientId = ingredient.IngredientId,
                TransactionType = "Restock",
                QuantityChanged = input.InitialStock,
                UnitCost = 0,
                PerformedByUserId = adminId,
                ReasonOrNote = "Khởi tạo tồn kho ban đầu",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = $"Đã thêm mới nguyên liệu '{ingredient.IngredientName}' ({ingredient.Unit}).";
        return RedirectToAction(nameof(Inventory));
    }
}

