using System.ComponentModel.DataAnnotations;
using CafeManagementSystem.Models.Entities;
namespace CafeManagementSystem.Models.ViewModels;

public class AdminDashboardViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetRevenue => GrossRevenue - RefundAmount;
    public int TotalOrders { get; set; }
    public int OnlineOrders { get; set; }
    public int CounterOrders { get; set; }
    public decimal OnlinePercent => TotalOrders == 0 ? 0 : 100m * OnlineOrders / TotalOrders;
    public decimal CounterPercent => TotalOrders == 0 ? 0 : 100m * CounterOrders / TotalOrders;
    public decimal RestockCost { get; set; }
    public int TotalComplaints { get; set; }
    public int PendingComplaints { get; set; }
    public double? AverageRating { get; set; }
    public List<AdminProductSalesRow> TopProducts { get; set; } = [];
    public List<CashierShift> CashierShifts { get; set; } = [];
    public List<AdminInventoryRow> Inventory { get; set; } = [];
}
public class AdminProductSalesRow
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
}
public class AdminInventoryRow
{
    public string IngredientName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal OrderDeduction { get; set; }
    public decimal Wastage { get; set; }
}
