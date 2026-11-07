using System.ComponentModel.DataAnnotations;
using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Models.ViewModels;

public class CustomerCartItem
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string SugarLevel { get; set; } = "100%";
    public string IceLevel { get; set; } = "100%";
    public string? ItemNote { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CustomerCheckoutViewModel
{
    public List<CustomerCartItem> CartItems { get; set; } = new();

    public decimal SubTotal => CartItems.Sum(c => c.LineTotal);

    public int AvailableLoyaltyPoints { get; set; }

    [Display(Name = "Điểm tích lũy sử dụng (1 điểm = 1.000đ)")]
    [Range(0, 1000000, ErrorMessage = "Điểm sử dụng không hợp lệ")]
    public int PointsUsed { get; set; }

    public decimal DiscountAmount => PointsUsed * 1000m;

    public decimal ShippingFee { get; set; } = 15000m;

    public decimal TotalAmount => Math.Max(0, SubTotal - DiscountAmount + ShippingFee);

    [Required(ErrorMessage = "Vui lòng nhập tên người nhận hàng")]
    [Display(Name = "Họ và tên người nhận")]
    [StringLength(100, ErrorMessage = "Tên không vượt quá 100 ký tự")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận")]
    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    [StringLength(20, ErrorMessage = "SĐT không vượt quá 20 ký tự")]
    public string RecipientPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    [Display(Name = "Địa chỉ nhận hàng chi tiết")]
    [StringLength(300, ErrorMessage = "Địa chỉ không vượt quá 300 ký tự")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán")]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD"; // "COD" hoặc "Transfer"

    [Display(Name = "Ghi chú cho đơn hàng")]
    [StringLength(300, ErrorMessage = "Ghi chú không vượt quá 300 ký tự")]
    public string? OrderNote { get; set; }
}

public class CustomerComplaintInputModel
{
    [Required(ErrorMessage = "Vui lòng chọn đơn hàng cần khiếu nại/phản hồi")]
    [Display(Name = "Mã đơn hàng")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại phản hồi")]
    [Display(Name = "Loại khiếu nại")]
    public string ComplaintType { get; set; } = "Chất lượng đồ uống";

    [Range(1, 5, ErrorMessage = "Vui lòng đánh giá từ 1 đến 5 sao")]
    [Display(Name = "Đánh giá (1 - 5 sao)")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi / khiếu nại")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Nội dung phản hồi từ 5 đến 500 ký tự")]
    [Display(Name = "Nội dung chi tiết")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Link hoặc URL ảnh minh chứng (tuỳ chọn)")]
    [StringLength(500, ErrorMessage = "URL ảnh không vượt quá 500 ký tự")]
    public string? EvidenceImageUrl { get; set; }
}

public class CustomerComplaintViewModel
{
    public List<Order> EligibleOrders { get; set; } = new();

    public List<Complaint> Complaints { get; set; } = new();

    public CustomerComplaintInputModel Form { get; set; } = new();
}

public class MyOrdersViewModel
{
    public List<Order> Orders { get; set; } = new();
}

