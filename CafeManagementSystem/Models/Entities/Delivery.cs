using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class Delivery
{
    public int DeliveryId { get; set; }

    public int OrderId { get; set; }

    public int DeliveryStaffId { get; set; }

    public string RecipientName { get; set; } = null!;

    public string RecipientPhone { get; set; } = null!;

    public string ShippingAddress { get; set; } = null!;

    public string DeliveryStatus { get; set; } = null!;

    public decimal CodAmount { get; set; }

    public bool IsCodCollected { get; set; }

    public int? HandedCodToShiftId { get; set; }

    public string? ProblemNote { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public virtual User DeliveryStaff { get; set; } = null!;

    public virtual CashierShift? HandedCodToShift { get; set; }

    public virtual Order Order { get; set; } = null!;
}
