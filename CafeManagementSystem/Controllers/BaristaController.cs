using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;
using System.Security.Claims;

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
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.ProductRecipes)
                        .ThenInclude(pr => pr.Ingredient)
            .Include(o => o.Customer)
            .Include(o => o.Barista)
            .Where(o => o.PreparationStatus == "Pending" || o.PreparationStatus == "Preparing")
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var viewModel = new KdsIndexViewModel
        {
            Orders = orders.Select(o => new KdsOrderCardViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                OrderSource = o.OrderSource,
                PreparationStatus = o.PreparationStatus,
                CreatedAt = o.CreatedAt,
                ElapsedMinutes = (int)(DateTime.Now - o.CreatedAt).TotalMinutes,
                OrderNote = o.OrderNote,
                CustomerName = o.Customer?.FullName,
                CustomerPhone = o.Customer?.Phone,
                BaristaName = o.Barista?.FullName,
                Items = o.OrderItems.Select(oi => new KdsOrderItemViewModel
                {
                    ProductName = oi.Variant?.Product?.ProductName ?? string.Empty,
                    SizeName = oi.Variant?.SizeName ?? string.Empty,
                    Quantity = oi.Quantity,
                    SugarLevel = oi.SugarLevel,
                    IceLevel = oi.IceLevel,
                    ItemNote = oi.ItemNote,
                    Recipes = (oi.Variant?.ProductRecipes ?? new List<ProductRecipe>())
                        .Select(pr => new KdsRecipeDetailViewModel
                        {
                            IngredientName = pr.Ingredient?.IngredientName ?? string.Empty,
                            Unit = pr.Ingredient?.Unit ?? string.Empty,
                            QuantityRequiredPerCup = pr.QuantityRequired,
                            TotalQuantityRequired = pr.QuantityRequired * oi.Quantity,
                            CurrentStock = pr.Ingredient?.StockQuantity ?? 0,
                            MinStockLevel = pr.Ingredient?.MinStockLevel ?? 0,
                            IsLowStock = (pr.Ingredient?.StockQuantity ?? 0) < ((pr.Ingredient?.MinStockLevel ?? 0) + (pr.QuantityRequired * oi.Quantity)),
                            PreparationGuide = pr.PreparationGuide
                        }).ToList()
                }).ToList()
            }).ToList(),
            TotalPendingCount = orders.Count(o => o.PreparationStatus == "Pending"),
            TotalPreparingCount = orders.Count(o => o.PreparationStatus == "Preparing"),
            TotalCupsWaiting = orders.Sum(o => o.OrderItems.Sum(oi => oi.Quantity)),
            LastRefreshed = DateTime.Now
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePreparationStatus(int orderId, string newStatus)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.ProductRecipes)
                        .ThenInclude(pr => pr.Ingredient)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(KDS));
        }

        var baristaIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(baristaIdClaim, out int baristaId);

        if (newStatus == "Preparing")
        {
            order.PreparationStatus = "Preparing";
            if (baristaId > 0)
            {
                order.BaristaId = baristaId;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã bắt đầu pha chế đơn #{order.OrderCode}.";
            return RedirectToAction(nameof(KDS));
        }

        if (newStatus == "Ready")
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Variant?.ProductRecipes == null) continue;
                    foreach (var recipe in item.Variant.ProductRecipes)
                    {
                        if (recipe.Ingredient == null) continue;
                        var totalUsed = recipe.QuantityRequired * item.Quantity;
                        recipe.Ingredient.StockQuantity -= totalUsed;
                        recipe.Ingredient.LastUpdated = DateTime.Now;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    IngredientId = recipe.Ingredient.IngredientId,
                    TransactionType = "OrderDeduction",
                    QuantityChanged = -totalUsed,
                    PerformedByUserId = baristaId > 0 ? baristaId : 1,
                    ReasonOrNote = $"Trừ kho tự động theo công thức cho đơn #{order.OrderCode}",
                    CreatedAt = DateTime.Now
                });
                    }
                }

                order.PreparationStatus = "Ready";
                if (order.OrderSource == "Counter")
                {
                    order.CompletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Đã hoàn tất đơn #{order.OrderCode} và trừ kho tự động.";
                return RedirectToAction(nameof(KDS));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi trừ kho: {ex.Message}";
                return RedirectToAction(nameof(KDS));
            }
        }

        TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
        return RedirectToAction(nameof(KDS));
    }

    [HttpGet]
    public async Task<IActionResult> Wastage()
    {
        var ingredients = await _context.Ingredients
            .OrderBy(i => i.IngredientName)
            .ToListAsync();

        var today = DateTime.Today;
        var todayTransactions = await _context.InventoryTransactions
            .Include(t => t.Ingredient)
            .Include(t => t.PerformedByUser)
            .Where(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1))
            .Where(t => t.TransactionType == "OrderDeduction" || t.TransactionType == "Wastage")
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var viewModel = new WastagePageViewModel
        {
            Ingredients = ingredients.Select(i => new IngredientStockViewModel
            {
                IngredientId = i.IngredientId,
                IngredientName = i.IngredientName,
                Unit = i.Unit,
                StockQuantity = i.StockQuantity,
                MinStockLevel = i.MinStockLevel,
                IsLowStock = i.StockQuantity < i.MinStockLevel,
                LastUpdated = i.LastUpdated
            }).ToList(),
            ReportForm = new WastageReportInputModel(),
            TodayTransactions = todayTransactions.Select(t => new InventoryTransactionViewModel
            {
                Time = t.CreatedAt,
                TransactionType = t.TransactionType,
                IngredientName = t.Ingredient?.IngredientName ?? string.Empty,
                QuantityChanged = t.QuantityChanged,
                PerformedByName = t.PerformedByUser?.FullName ?? "Hệ thống",
                ReasonOrNote = t.ReasonOrNote
            }).ToList(),
            Summary = new WastageSummaryViewModel
            {
                TotalIngredientTypes = ingredients.Count,
                LowStockCount = ingredients.Count(i => i.StockQuantity < i.MinStockLevel),
                TodayWastageCount = todayTransactions.Count(t => t.TransactionType == "Wastage")
            }
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportWastage(WastageReportInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu báo cáo hao hụt không hợp lệ.";
            return RedirectToAction(nameof(Wastage));
        }

        if (model.WastedQuantity <= 0)
        {
            TempData["ErrorMessage"] = "Số lượng hao hụt phải lớn hơn 0.";
            return RedirectToAction(nameof(Wastage));
        }

        var baristaIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(baristaIdClaim, out int baristaId);

        var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngredientId == model.IngredientId);
        if (ingredient == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nguyên liệu.";
            return RedirectToAction(nameof(Wastage));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            ingredient.StockQuantity -= model.WastedQuantity;
            ingredient.LastUpdated = DateTime.Now;

            var reasonNote = string.IsNullOrWhiteSpace(model.Note)
                ? model.Reason
                : $"{model.Reason} - {model.Note}";

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                IngredientId = ingredient.IngredientId,
                TransactionType = "Wastage",
                QuantityChanged = -model.WastedQuantity,
                PerformedByUserId = baristaId > 0 ? baristaId : 1,
                ReasonOrNote = reasonNote,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Đã ghi nhận báo cáo hao hụt thành công.";
            return RedirectToAction(nameof(Wastage));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            return RedirectToAction(nameof(Wastage));
        }
    }
}
