using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Models.ViewModels;

public class PosViewModel
{
    public List<Category> Categories { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public int? SelectedCategoryId { get; set; }

    public User? SelectedCustomer { get; set; }
    public string? SearchPhone { get; set; }

    public List<PosCartItem> CartItems { get; set; } = new();

    public decimal SubTotal => CartItems.Sum(c => c.LineTotal);
    public int PointsUsed { get; set; }
    public int MaxPointsUsable => SelectedCustomer != null ? Math.Min(SelectedCustomer.LoyaltyPoints, (int)(SubTotal / 1000)) : 0;
    public decimal DiscountAmount => PointsUsed * 1000m;
    public decimal TotalAmount => Math.Max(0, SubTotal - DiscountAmount);

    public string PaymentMethod { get; set; } = "Cash";
    public string? OrderNote { get; set; }

    public CashierShift? CurrentShift { get; set; }
}
