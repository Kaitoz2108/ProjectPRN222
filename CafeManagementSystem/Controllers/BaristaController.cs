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
        var baristaIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(baristaIdClaim, out int baristaId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.ProductRecipes)
                            .ThenInclude(pr => pr.Ingredient)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(KDS));
            }

            // Chặn chuyển đổi trạng thái nếu đơn đã Ready, Completed hoặc Cancelled (Issue 5)
            if (order.PreparationStatus == "Ready" || order.PreparationStatus == "Completed" || order.PreparationStatus == "Cancelled")
            {
                await transaction.RollbackAsync();
                TempData["WarningMessage"] = $"Đơn hàng #{order.OrderCode} hiện đang ở trạng thái '{order.PreparationStatus}', không thể thay đổi thêm.";
                return RedirectToAction(nameof(KDS));
            }

            if (newStatus == "Preparing")
            {
                if (order.PreparationStatus != "Pending")
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Chỉ có thể chuyển sang 'Đang pha chế' từ trạng thái 'Chờ nhận' (Pending).";
                    return RedirectToAction(nameof(KDS));
                }

                order.PreparationStatus = "Preparing";
                if (baristaId > 0)
                {
                    order.BaristaId = baristaId;
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Đã bắt đầu pha chế đơn #{order.OrderCode}.";
                return RedirectToAction(nameof(KDS));
            }

            if (newStatus == "Ready")
            {
                // Cho phép hoàn tất từ Pending hoặc Preparing
                if (order.PreparationStatus != "Pending" && order.PreparationStatus != "Preparing")
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Trạng thái đơn hàng không hợp lệ để hoàn tất pha chế.";
                    return RedirectToAction(nameof(KDS));
                }

                // 1. Cộng gộp nhu cầu nguyên liệu cần dùng cho toàn bộ đơn hàng
                var requiredIngredients = new Dictionary<int, (Ingredient Ingredient, decimal TotalNeeded)>();
                var itemsWithoutRecipe = new List<string>();

                foreach (var item in order.OrderItems)
                {
                    if (item.Variant?.ProductRecipes == null || !item.Variant.ProductRecipes.Any())
                    {
                        var prodName = item.Variant?.Product?.ProductName ?? $"Món #{item.VariantId}";
                        var size = item.Variant?.SizeName ?? "";
                        itemsWithoutRecipe.Add($"{prodName} ({size})");
                        continue;
                    }

                    foreach (var recipe in item.Variant.ProductRecipes)
                    {
                        if (recipe.Ingredient == null) continue;
                        var ingId = recipe.IngredientId;
                        var needed = recipe.QuantityRequired * item.Quantity;

                        if (requiredIngredients.TryGetValue(ingId, out var existing))
                        {
                            requiredIngredients[ingId] = (existing.Ingredient, existing.TotalNeeded + needed);
                        }
                        else
                        {
                            requiredIngredients[ingId] = (recipe.Ingredient, needed);
                        }
                    }
                }

                // 2. Kiểm tra tồn kho trước khi trừ (chặn số âm - Issue 6)
                var insufficientList = new List<string>();
                foreach (var (ingId, (ingredient, totalNeeded)) in requiredIngredients)
                {
                    if (ingredient.StockQuantity < totalNeeded)
                    {
                        insufficientList.Add($"{ingredient.IngredientName} (Cần: {totalNeeded:N2} {ingredient.Unit}, Hiện còn: {ingredient.StockQuantity:N2} {ingredient.Unit})");
                    }
                }

                if (insufficientList.Any())
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Không đủ nguyên liệu để hoàn tất đơn #{order.OrderCode}: " + string.Join("; ", insufficientList);
                    return RedirectToAction(nameof(KDS));
                }

                // 3. Thực hiện trừ kho và ghi nhận InventoryTransactions (chỉ 1 lần duy nhất)
                foreach (var (ingId, (ingredient, totalNeeded)) in requiredIngredients)
                {
                    ingredient.StockQuantity -= totalNeeded;
                    ingredient.LastUpdated = DateTime.Now;

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        IngredientId = ingredient.IngredientId,
                        TransactionType = "OrderDeduction",
                        QuantityChanged = -totalNeeded,
                        PerformedByUserId = baristaId > 0 ? baristaId : 1,
                        ReasonOrNote = $"Trừ kho tự động theo công thức cho đơn #{order.OrderCode}",
                        CreatedAt = DateTime.Now
                    });
                }

                order.PreparationStatus = "Ready";
                if (baristaId > 0 && order.BaristaId == null)
                {
                    order.BaristaId = baristaId;
                }

                if (order.OrderSource == "Counter")
                {
                    order.CompletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var successMsg = $"Đã hoàn tất đơn #{order.OrderCode} và trừ kho tự động.";
                if (itemsWithoutRecipe.Any())
                {
                    successMsg += $" (Lưu ý: {string.Join(", ", itemsWithoutRecipe)} chưa cấu hình công thức nên không trừ kho).";
                }
                TempData["SuccessMessage"] = successMsg;
                return RedirectToAction(nameof(KDS));
            }

            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
            return RedirectToAction(nameof(KDS));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            return RedirectToAction(nameof(KDS));
        }
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
    public async Task<IActionResult> ReportWastage([Bind(Prefix = "ReportForm")] WastageReportInputModel model)
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

        if (ingredient.StockQuantity < model.WastedQuantity)
        {
            TempData["ErrorMessage"] = $"Số lượng hao hụt ({model.WastedQuantity:N2} {ingredient.Unit}) vượt quá tồn kho hiện tại ({ingredient.StockQuantity:N2} {ingredient.Unit}).";
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
