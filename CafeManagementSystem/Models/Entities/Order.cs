using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int? CustomerId { get; set; }

    public int? CashierId { get; set; }

    public int? CashierShiftId { get; set; }

    public int? BaristaId { get; set; }

    public string OrderSource { get; set; } = null!;

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public int LoyaltyPointsUsed { get; set; }

    public int LoyaltyPointsEarned { get; set; }

    public string PreparationStatus { get; set; } = null!;

    public string? OrderNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual User? Barista { get; set; }

    public virtual User? Cashier { get; set; }

    public virtual CashierShift? CashierShift { get; set; }

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual User? Customer { get; set; }

    public virtual Delivery? Delivery { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
