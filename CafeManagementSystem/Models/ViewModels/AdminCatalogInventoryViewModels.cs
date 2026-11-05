using System.ComponentModel.DataAnnotations;
using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Models.ViewModels;

public class AdminProductsViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Ingredient> Ingredients { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
}

public class AdminCreateProductInput
{
    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [StringLength(100, ErrorMessage = "Tên không vượt quá 100 ký tự")]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả không vượt quá 500 ký tự")]
    public string? Description { get; set; }

    [StringLength(255, ErrorMessage = "Đường dẫn ảnh không vượt quá 255 ký tự")]
    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public class AdminSaveVariantInput
{
    public int? VariantId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên kích cỡ (S, M, L...)")]
    [StringLength(20)]
    public string SizeName { get; set; } = "M";

    [Required(ErrorMessage = "Vui lòng nhập giá bán")]
    [Range(0, 10000000, ErrorMessage = "Giá bán không hợp lệ")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AdminSaveRecipeInput
{
    [Required]
    public int VariantId { get; set; }

    [Required]
    public int IngredientId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập định lượng nguyên liệu")]
    [Range(0.01, 100000, ErrorMessage = "Định lượng phải lớn hơn 0")]
    public decimal QuantityRequired { get; set; }

    [StringLength(500, ErrorMessage = "Hướng dẫn không vượt quá 500 ký tự")]
    public string? PreparationGuide { get; set; }
}

public class AdminInventoryViewModel
{
    public List<Ingredient> Ingredients { get; set; } = new();
    public List<InventoryTransaction> RecentTransactions { get; set; } = new();
    public int LowStockCount => Ingredients.Count(i => i.StockQuantity <= i.MinStockLevel);
    public decimal TotalStockItems => Ingredients.Sum(i => i.StockQuantity);
}

public class AdminRestockInput
{
    [Required(ErrorMessage = "Vui lòng chọn nguyên liệu cần nhập kho")]
    public int IngredientId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng nhập thêm")]
    [Range(0.01, 1000000, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đơn giá nhập kho")]
    [Range(0, 100000000, ErrorMessage = "Đơn giá không hợp lệ")]
    public decimal UnitCost { get; set; }

    [StringLength(300, ErrorMessage = "Ghi chú không vượt quá 300 ký tự")]
    public string? ReasonOrNote { get; set; }
}

public class AdminCreateIngredientInput
{
    [Required(ErrorMessage = "Vui lòng nhập tên nguyên liệu")]
    [StringLength(100)]
    public string IngredientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính (gram, ml, miếng...)")]
    [StringLength(30)]
    public string Unit { get; set; } = "ml";

    [Range(0, 1000000)]
    public decimal InitialStock { get; set; } = 0;

    [Range(0, 1000000)]
    public decimal MinStockLevel { get; set; } = 100;
}

