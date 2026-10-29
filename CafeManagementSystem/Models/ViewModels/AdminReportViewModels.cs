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

public class AdminStaffScheduleViewModel
{
    public List<User> Staff { get; set; } = [];
    public List<WorkSchedule> Schedules { get; set; } = [];
    public List<Attendance> Attendances { get; set; } = [];
    public AdminCreateStaffInput NewStaff { get; set; } = new();
    public AdminCreateScheduleInput NewSchedule { get; set; } = new();
    public AdminAttendanceInput AttendanceEdit { get; set; } = new();
}
public class AdminCreateStaffInput
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(20)] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, RegularExpression("^(Cashier|Barista|DeliveryStaff)$")] public string Role { get; set; } = "Cashier";
    public bool IsActive { get; set; } = true;
}
public class AdminCreateScheduleInput
{
    [Range(1, int.MaxValue)] public int StaffId { get; set; }
    public DateOnly WorkDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required, RegularExpression("^(Morning|Afternoon|Evening)$")] public string ShiftName { get; set; } = "Morning";
    [StringLength(255)] public string? Note { get; set; }
}
public class AdminAttendanceInput
{
    [Range(1, int.MaxValue)] public int AttendanceId { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    [Required, RegularExpression("^(Present|Late|Absent|Leave)$")] public string Status { get; set; } = "Present";
    [StringLength(255)] public string? Note { get; set; }
}

public class AdminComplaintsViewModel
{
    public List<Complaint> Complaints { get; set; } = [];
    public AdminResolveComplaintInput Resolution { get; set; } = new();
}
public class AdminResolveComplaintInput
{
    [Range(1, int.MaxValue)] public int ComplaintId { get; set; }
    [Required, StringLength(1000)] public string AdminResponse { get; set; } = "";
    [Required, RegularExpression("^(Resolved|Rejected)$")] public string Status { get; set; } = "Resolved";
    public bool IsRefundApproved { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal RefundAmount { get; set; }
}
