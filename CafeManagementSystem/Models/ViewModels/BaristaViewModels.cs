using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CafeManagementSystem.Models.ViewModel;

public class KdsIndexViewModel
{
    public List<KdsOrderCardViewModel> Orders { get; set; } = new();
    public int TotalPendingCount { get; set; }
    public int TotalPreparingCount { get; set; }
    public int TotalCupsWaiting { get; set; }
    public DateTime LastRefreshed { get; set; }
}

public class KdsOrderCardViewModel
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderSource { get; set; } = string.Empty;
    public string PreparationStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ElapsedMinutes { get; set; }
    public string? OrderNote { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? BaristaName { get; set; }

    public List<KdsOrderItemViewModel> Items { get; set; } = new();
}

public class KdsOrderItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SugarLevel { get; set; }
    public string? IceLevel { get; set; }
    public string? ItemNote { get; set; }

    public List<KdsRecipeDetailViewModel> Recipes { get; set; } = new();
}

public class KdsRecipeDetailViewModel
{
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal QuantityRequiredPerCup { get; set; }
    public decimal TotalQuantityRequired { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinStockLevel { get; set; }
    public bool IsLowStock { get; set; }
    public string? PreparationGuide { get; set; }
}

public class WastagePageViewModel
{
    public List<IngredientStockViewModel> Ingredients { get; set; } = new();
    public WastageReportInputModel ReportForm { get; set; } = new();
    public List<InventoryTransactionViewModel> TodayTransactions { get; set; } = new();
    public WastageSummaryViewModel Summary { get; set; } = new();
}

public class WastageSummaryViewModel
{
    public int TotalIngredientTypes { get; set; }
    public int LowStockCount { get; set; }
    public int TodayWastageCount { get; set; }
}

public class IngredientStockViewModel
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal StockQuantity { get; set; }
    public decimal MinStockLevel { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class WastageReportInputModel
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nguyên liệu.")]
    public int IngredientId { get; set; }

    [Required]
    [Range(0.01, 999999.0, ErrorMessage = "Số lượng hao hụt phải lớn hơn 0.")]
    public decimal WastedQuantity { get; set; }

    [Required]
    [StringLength(50)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Note { get; set; }
}

public class InventoryTransactionViewModel
{
    public DateTime Time { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityChanged { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public string? ReasonOrNote { get; set; }
}