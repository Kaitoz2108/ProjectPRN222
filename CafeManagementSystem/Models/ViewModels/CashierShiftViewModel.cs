using System.ComponentModel.DataAnnotations;
using CafeManagementSystem.Models.Entities;

namespace CafeManagementSystem.Models.ViewModels;

public class CashierShiftViewModel
{
    public bool HasOpenShift { get; set; }

    public CashierShift? CurrentShift { get; set; }

    public CashierShift? PreviousShift { get; set; }

    public CashierShift? LastClosedShift { get; set; }

    public List<Order> ShiftCashOrders { get; set; } = new();

    public decimal TotalCashSales => ShiftCashOrders.Sum(o => o.TotalAmount);

    public decimal ExpectedCash => (CurrentShift?.OpeningCash ?? 0) + (CurrentShift?.SystemCashSales ?? 0);

    // Form inputs for OpenShift
    [Display(Name = "Số tiền mặt đầu ca (VNĐ)")]
    [Range(0, 100000000, ErrorMessage = "Số tiền mặt đầu ca không hợp lệ")]
    public decimal InputOpeningCash { get; set; }

    [Display(Name = "Xác nhận đã kiểm đếm tiền mặt đầu ca")]
    public bool IsOpeningCashConfirmed { get; set; }

    [Display(Name = "Ghi chú nhận ca")]
    public string? InputOpeningNotes { get; set; }

    // Form inputs for CloseShift
    [Display(Name = "Số tiền mặt thực tế trong két cuối ca (VNĐ)")]
    [Range(0, 100000000, ErrorMessage = "Số tiền thực tế cuối ca không hợp lệ")]
    public decimal? ClosingActualCash { get; set; }

    [Display(Name = "Ghi chú bàn giao ca")]
    [StringLength(500, ErrorMessage = "Ghi chú không vượt quá 500 ký tự")]
    public string? HandoverNotes { get; set; }
}
