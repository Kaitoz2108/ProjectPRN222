using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class CashierShift
{
    public int ShiftId { get; set; }

    public int CashierId { get; set; }

    public int? PreviousShiftId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal OpeningCash { get; set; }

    public bool IsOpeningCashConfirmed { get; set; }

    public decimal SystemCashSales { get; set; }

    public decimal? ClosingActualCash { get; set; }

    public decimal? Discrepancy { get; set; }

    public string? HandoverNotes { get; set; }

    public string ShiftStatus { get; set; } = null!;

    public virtual User Cashier { get; set; } = null!;

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<CashierShift> InversePreviousShift { get; set; } = new List<CashierShift>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual CashierShift? PreviousShift { get; set; }
}
