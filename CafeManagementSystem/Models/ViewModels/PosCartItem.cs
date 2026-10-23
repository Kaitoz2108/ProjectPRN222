namespace CafeManagementSystem.Models.ViewModels;

public class PosCartItem
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public string SugarLevel { get; set; } = "100%";
    public string IceLevel { get; set; } = "100%";
    public string? ItemNote { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
