using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class PaymentTransaction
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? GatewayTransactionRef { get; set; }

    public string? GatewayResponseCode { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
