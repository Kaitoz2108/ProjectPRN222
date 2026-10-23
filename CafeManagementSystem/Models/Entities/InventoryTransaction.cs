using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class InventoryTransaction
{
    public int InventoryTransId { get; set; }

    public int IngredientId { get; set; }

    public int PerformedByUserId { get; set; }

    public string TransactionType { get; set; } = null!;

    public decimal QuantityChanged { get; set; }

    public decimal? UnitCost { get; set; }

    public string? ReasonOrNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual User PerformedByUser { get; set; } = null!;
}
