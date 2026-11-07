namespace CafeManagementSystem.Models.ViewModels;

public class LandingPageViewModel
{
    /// <summary>
    /// 3 món signature hiển thị nổi bật ở hàng trên theo đúng ảnh tham chiếu
    /// </summary>
    public List<LandingFeaturedProductItem> FeaturedProducts { get; set; } = new();

    /// <summary>
    /// Toàn bộ sản phẩm đang bán được đồng bộ trực tiếp từ Database
    /// </summary>
    public List<LandingFeaturedProductItem> AllProducts { get; set; } = new();

    /// <summary>
    /// Danh mục sản phẩm từ Database
    /// </summary>
    public List<LandingCategoryItem> Categories { get; set; } = new();
}

public class LandingCategoryItem
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}

public class LandingFeaturedProductItem
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
}
