using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string SizeName { get; set; } = null!;

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductRecipe> ProductRecipes { get; set; } = new List<ProductRecipe>();
}
