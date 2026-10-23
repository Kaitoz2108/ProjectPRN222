using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public string IngredientName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal StockQuantity { get; set; }

    public decimal MinStockLevel { get; set; }

    public DateTime LastUpdated { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<ProductRecipe> ProductRecipes { get; set; } = new List<ProductRecipe>();
}
