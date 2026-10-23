using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class ProductRecipe
{
    public int RecipeId { get; set; }

    public int VariantId { get; set; }

    public int IngredientId { get; set; }

    public decimal QuantityRequired { get; set; }

    public string? PreparationGuide { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
