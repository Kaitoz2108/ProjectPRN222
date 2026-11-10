using System.Security.Claims;
using CafeManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagementSystem.Controllers;

public partial class AdminController
{
    [HttpGet]
    public async Task<IActionResult> Products()
    {
        ViewBag.Categories = await _context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.CategoryName)
            .ToListAsync();
        ViewBag.Ingredients = await _context.Ingredients
            .AsNoTracking()
            .OrderBy(ingredient => ingredient.IngredientName)
            .ToListAsync();

        var products = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.ProductVariants)
                .ThenInclude(variant => variant.ProductRecipes)
                    .ThenInclude(recipe => recipe.Ingredient)
            .OrderBy(product => product.Category.CategoryName)
            .ThenBy(product => product.ProductName)
            .ToListAsync();

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string categoryName, string? description)
    {
        categoryName = categoryName?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (categoryName.Length is 0 or > 100 || description?.Length > 255)
        {
            TempData["ErrorMessage"] = "Tên danh mục và mô tả không hợp lệ.";
            return RedirectToAction(nameof(Products));
        }

        if (await _context.Categories.AnyAsync(category => category.CategoryName == categoryName))
        {
            TempData["ErrorMessage"] = "Danh mục này đã tồn tại.";
            return RedirectToAction(nameof(Products));
        }

        _context.Categories.Add(new Category
        {
            CategoryName = categoryName,
            Description = description,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã thêm danh mục {categoryName}.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(int categoryId, string productName, string? description, string? imageUrl)
    {
        productName = productName?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

        if (productName.Length is 0 or > 150 || description?.Length > 500 || imageUrl?.Length > 500)
        {
            TempData["ErrorMessage"] = "Thông tin món nước không hợp lệ.";
            return RedirectToAction(nameof(Products));
        }

        if (!await _context.Categories.AnyAsync(category => category.CategoryId == categoryId && category.IsActive))
        {
            TempData["ErrorMessage"] = "Danh mục không tồn tại hoặc đã bị khóa.";
            return RedirectToAction(nameof(Products));
        }

        _context.Products.Add(new Product
        {
            CategoryId = categoryId,
            ProductName = productName,
            Description = description,
            ImageUrl = imageUrl,
            IsAvailable = true,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã thêm món {productName}.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductAvailability(int productId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(item => item.ProductId == productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Products));
        }

        product.IsAvailable = !product.IsAvailable;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã {(product.IsAvailable ? "bật bán" : "tắt bán")} món {product.ProductName}.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(int productId, string sizeName, decimal price)
    {
        sizeName = sizeName?.Trim().ToUpperInvariant() ?? string.Empty;
        if (sizeName is not ("S" or "M" or "L") || price <= 0)
        {
            TempData["ErrorMessage"] = "Size chỉ được là S, M hoặc L và giá phải lớn hơn 0.";
            return RedirectToAction(nameof(Products));
        }

        if (!await _context.Products.AnyAsync(product => product.ProductId == productId))
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Products));
        }

        if (await _context.ProductVariants.AnyAsync(variant => variant.ProductId == productId && variant.SizeName == sizeName))
        {
            TempData["ErrorMessage"] = $"Sản phẩm đã có size {sizeName}.";
            return RedirectToAction(nameof(Products));
        }

        _context.ProductVariants.Add(new ProductVariant
        {
            ProductId = productId,
            SizeName = sizeName,
            Price = price,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã thêm size {sizeName}.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRecipe(int variantId, int ingredientId, decimal quantityRequired, string? preparationGuide)
    {
        preparationGuide = string.IsNullOrWhiteSpace(preparationGuide) ? null : preparationGuide.Trim();
        if (quantityRequired <= 0 || preparationGuide?.Length > 500)
        {
            TempData["ErrorMessage"] = "Định lượng phải lớn hơn 0 và hướng dẫn tối đa 500 ký tự.";
            return RedirectToAction(nameof(Products));
        }

        var variantExists = await _context.ProductVariants.AnyAsync(variant => variant.VariantId == variantId);
        var ingredientExists = await _context.Ingredients.AnyAsync(ingredient => ingredient.IngredientId == ingredientId);
        if (!variantExists || !ingredientExists)
        {
            TempData["ErrorMessage"] = "Size hoặc nguyên liệu không tồn tại.";
            return RedirectToAction(nameof(Products));
        }

        var recipe = await _context.ProductRecipes
            .FirstOrDefaultAsync(item => item.VariantId == variantId && item.IngredientId == ingredientId);
        if (recipe == null)
        {
            _context.ProductRecipes.Add(new ProductRecipe
            {
                VariantId = variantId,
                IngredientId = ingredientId,
                QuantityRequired = quantityRequired,
                PreparationGuide = preparationGuide
            });
        }
        else
        {
            recipe.QuantityRequired = quantityRequired;
            recipe.PreparationGuide = preparationGuide;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã lưu công thức.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRecipe(int recipeId)
    {
        var recipe = await _context.ProductRecipes.FirstOrDefaultAsync(item => item.RecipeId == recipeId);
        if (recipe == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy công thức.";
            return RedirectToAction(nameof(Products));
        }

        _context.ProductRecipes.Remove(recipe);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa nguyên liệu khỏi công thức.";
        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public async Task<IActionResult> Inventory()
    {
        var ingredients = await _context.Ingredients
            .AsNoTracking()
            .OrderBy(ingredient => ingredient.IngredientName)
            .ToListAsync();
        ViewBag.Ingredients = ingredients;

        var transactions = await _context.InventoryTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Ingredient)
            .Include(transaction => transaction.PerformedByUser)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Take(200)
            .ToListAsync();

        ViewBag.Transactions = transactions;
        return View(ingredients);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIngredient(string ingredientName, string unit, decimal minStockLevel)
    {
        ingredientName = ingredientName?.Trim() ?? string.Empty;
        unit = unit?.Trim() ?? string.Empty;
        if (ingredientName.Length is 0 or > 150 || unit.Length is 0 or > 30 || minStockLevel < 0)
        {
            TempData["ErrorMessage"] = "Thông tin nguyên liệu không hợp lệ.";
            return RedirectToAction(nameof(Inventory));
        }

        if (await _context.Ingredients.AnyAsync(ingredient => ingredient.IngredientName == ingredientName))
        {
            TempData["ErrorMessage"] = "Nguyên liệu này đã tồn tại.";
            return RedirectToAction(nameof(Inventory));
        }

        _context.Ingredients.Add(new Ingredient
        {
            IngredientName = ingredientName,
            Unit = unit,
            StockQuantity = 0,
            MinStockLevel = minStockLevel,
            LastUpdated = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã thêm nguyên liệu {ingredientName}.";
        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restock(int ingredientId, decimal quantityAdded, decimal unitCost, string? reasonOrNote)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Forbid();
        }

        reasonOrNote = string.IsNullOrWhiteSpace(reasonOrNote) ? null : reasonOrNote.Trim();
        if (quantityAdded <= 0 || unitCost < 0 || reasonOrNote?.Length > 300)
        {
            TempData["ErrorMessage"] = "Số lượng nhập phải lớn hơn 0, đơn giá không âm và ghi chú tối đa 300 ký tự.";
            return RedirectToAction(nameof(Inventory));
        }

        var ingredient = await _context.Ingredients.FirstOrDefaultAsync(item => item.IngredientId == ingredientId);
        if (ingredient == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nguyên liệu.";
            return RedirectToAction(nameof(Inventory));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        ingredient.StockQuantity += quantityAdded;
        ingredient.LastUpdated = DateTime.Now;
        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            IngredientId = ingredient.IngredientId,
            PerformedByUserId = adminId,
            TransactionType = "Restock",
            QuantityChanged = quantityAdded,
            UnitCost = unitCost,
            ReasonOrNote = reasonOrNote,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Đã nhập thêm {quantityAdded:N2} {ingredient.Unit} {ingredient.IngredientName}.";
        return RedirectToAction(nameof(Inventory));
    }

    private bool TryGetAdminId(out int adminId)
    {
        adminId = 0;
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out adminId) && adminId > 0;
    }
}
