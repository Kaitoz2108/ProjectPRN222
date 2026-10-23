using System.ComponentModel.DataAnnotations;

namespace CafeManagementSystem.Models.ViewModels;

public class ProfileViewModel
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email không vượt quá 100 ký tự")]
    [Display(Name = "Địa chỉ Email")]
    public string? Email { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ không vượt quá 255 ký tự")]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Vai trò")]
    public string RoleName { get; set; } = string.Empty;

    [Display(Name = "Điểm tích lũy")]
    public int LoyaltyPoints { get; set; }

    [Display(Name = "Ngày tạo tài khoản")]
    public DateTime CreatedAt { get; set; }
}
