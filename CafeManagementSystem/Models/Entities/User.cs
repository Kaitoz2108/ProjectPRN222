using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? Address { get; set; }

    public int LoyaltyPoints { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<CashierShift> CashierShifts { get; set; } = new List<CashierShift>();

    public virtual ICollection<Complaint> ComplaintCustomers { get; set; } = new List<Complaint>();

    public virtual ICollection<Complaint> ComplaintResolvedByAdmins { get; set; } = new List<Complaint>();

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<Order> OrderBarista { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderCashiers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderCustomers { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<WorkSchedule> WorkScheduleCreatedByAdmins { get; set; } = new List<WorkSchedule>();

    public virtual ICollection<WorkSchedule> WorkScheduleStaffs { get; set; } = new List<WorkSchedule>();
}
