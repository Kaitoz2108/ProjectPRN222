using System.Data;
using System.Security.Claims;
using CafeManagementSystem.Models.Entities;
using CafeManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagementSystem.Controllers;

public partial class AdminController
{
    // Prefer this explicit route to the existing conventional Dashboard action.
    [HttpGet("/Admin/Dashboard", Order = -1)]
    [ActionName("Dashboard")]
    public async Task<IActionResult> OperationsDashboard(DateTime? fromDate, DateTime? toDate)
    {
        var from = (fromDate ?? DateTime.Today.AddDays(-29)).Date;
        var to = (toDate ?? DateTime.Today).Date;
        if (from > to || to == DateTime.MaxValue.Date)
            ModelState.AddModelError("", "Khoảng ngày không hợp lệ: ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        var model = new AdminDashboardViewModel { FromDate = from, ToDate = to };
        if (!ModelState.IsValid) return View("Dashboard", model);
        var end = to.AddDays(1);
        var orders = _context.Orders.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.TotalOrders = await orders.CountAsync();
        model.OnlineOrders = await orders.CountAsync(x => x.OrderSource == "Online");
        model.CounterOrders = await orders.CountAsync(x => x.OrderSource == "Counter");
        // Cash basis: receipts and refunds by transaction date, including older orders.
        var payments = _context.PaymentTransactions.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.GrossRevenue = await payments.Where(x => x.TransactionType == "Payment" && x.PaymentStatus == "Success").SumAsync(x => (decimal?)x.Amount) ?? 0;
        model.RefundAmount = await payments.Where(x => x.TransactionType == "Refund" && x.PaymentStatus == "Refunded").SumAsync(x => (decimal?)x.Amount) ?? 0;
        model.TopProducts = await _context.OrderItems.AsNoTracking()
            .Where(x => x.Order.CreatedAt >= from && x.Order.CreatedAt < end && x.Order.PreparationStatus != "Cancelled"
                && x.Order.PaymentTransactions.Any(p => p.TransactionType == "Payment" && p.PaymentStatus == "Success"))
            .GroupBy(x => new { x.Variant.ProductId, x.Variant.Product.ProductName })
            .Select(g => new AdminProductSalesRow { ProductName = g.Key.ProductName, Quantity = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Quantity).ThenBy(x => x.ProductName).Take(5).ToListAsync();
        model.CashierShifts = await _context.CashierShifts.AsNoTracking().Include(x => x.Cashier)
            .Where(x => x.StartTime >= from && x.StartTime < end).OrderByDescending(x => x.StartTime).ToListAsync();
        var inventory = _context.InventoryTransactions.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.RestockCost = await inventory.Where(x => x.TransactionType == "Restock")
            .SumAsync(x => (decimal?)(Math.Abs(x.QuantityChanged) * (x.UnitCost ?? 0))) ?? 0;
        // Keep quantities separated by ingredient/unit.
        model.Inventory = await inventory.Where(x => x.TransactionType == "OrderDeduction" || x.TransactionType == "Wastage")
            .GroupBy(x => new { x.IngredientId, x.Ingredient.IngredientName, x.Ingredient.Unit })
            .Select(g => new AdminInventoryRow {
                IngredientName = g.Key.IngredientName, Unit = g.Key.Unit,
                OrderDeduction = g.Sum(x => x.TransactionType == "OrderDeduction" ? Math.Abs(x.QuantityChanged) : 0),
                Wastage = g.Sum(x => x.TransactionType == "Wastage" ? Math.Abs(x.QuantityChanged) : 0)
            }).OrderBy(x => x.IngredientName).ToListAsync();
        var complaints = _context.Complaints.AsNoTracking().Where(x => x.CreatedAt >= from && x.CreatedAt < end);
        model.TotalComplaints = await complaints.CountAsync();
        model.PendingComplaints = await complaints.CountAsync(x => x.ComplaintStatus == "Pending");
        model.AverageRating = await complaints.AverageAsync(x => (double?)x.Rating);
        return View("Dashboard", model);
    }
}

public partial class AdminController
{
    private static readonly string[] OperationsStaffRoles = ["Cashier", "Barista", "DeliveryStaff"];

    private async Task<bool> IsOperationsAdminAsync() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        && await _context.Users.AnyAsync(x => x.UserId == id && x.IsActive && x.Role.RoleName == "Admin");

    private async Task<AdminStaffScheduleViewModel> LoadStaffScheduleAsync(AdminStaffScheduleViewModel? model = null)
    {
        model ??= new();
        model.Staff = await _context.Users.AsNoTracking().Include(x => x.Role)
            .Where(x => OperationsStaffRoles.Contains(x.Role.RoleName)).OrderBy(x => x.FullName).ToListAsync();
        model.Schedules = await _context.WorkSchedules.AsNoTracking().Include(x => x.Staff)
            .OrderByDescending(x => x.WorkDate).ThenBy(x => x.StartTime).ToListAsync();
        model.Attendances = await _context.Attendances.AsNoTracking().Include(x => x.Staff).Include(x => x.Schedule)
            .OrderByDescending(x => x.Schedule.WorkDate).ThenBy(x => x.Schedule.StartTime).ToListAsync();
        return model;
    }

    [HttpGet]
    public async Task<IActionResult> StaffSchedule() => View(await LoadStaffScheduleAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff([Bind(Prefix = "NewStaff")] AdminCreateStaffInput input)
    {
        if (!await IsOperationsAdminAsync()) return Forbid();
        input.Phone = input.Phone.Trim();
        input.FullName = input.FullName.Trim();
        input.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        var role = await _context.Roles.SingleOrDefaultAsync(x => x.RoleName == input.Role && OperationsStaffRoles.Contains(x.RoleName));
        if (role == null) ModelState.AddModelError("NewStaff.Role", "Vai trò nhân viên không hợp lệ hoặc chưa được cấu hình.");
        if (await _context.Users.AnyAsync(x => x.Phone == input.Phone)) ModelState.AddModelError("NewStaff.Phone", "Số điện thoại đã tồn tại.");
        if (input.Email != null && await _context.Users.AnyAsync(x => x.Email == input.Email)) ModelState.AddModelError("NewStaff.Email", "Email đã tồn tại.");
        if (ModelState.IsValid)
        {
            // Match AccountController's existing credential format. Hashing requires a coordinated auth migration.
            _context.Users.Add(new User { FullName = input.FullName, Phone = input.Phone, Email = input.Email,
                PasswordHash = input.Password, RoleId = role!.RoleId, IsActive = input.IsActive, CreatedAt = DateTime.Now });
            try
            {
                await _context.SaveChangesAsync();
                TempData["OperationsSuccess"] = "Đã tạo tài khoản nhân viên.";
                return RedirectToAction(nameof(StaffSchedule));
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                ModelState.AddModelError("", "Không thể lưu tài khoản. Kiểm tra email/số điện thoại trùng và thử lại.");
            }
        }
        input.Password = "";
        ModelState.Remove("NewStaff.Password");
        return View("StaffSchedule", await LoadStaffScheduleAsync(new() { NewStaff = input }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStaffActive(int staffId, bool isActive)
    {
        if (!await IsOperationsAdminAsync()) return Forbid();
        var staff = await _context.Users.SingleOrDefaultAsync(x => x.UserId == staffId && OperationsStaffRoles.Contains(x.Role.RoleName));
        if (staff == null) return NotFound();
        staff.IsActive = isActive;
        await _context.SaveChangesAsync();
        TempData["OperationsSuccess"] = "Đã cập nhật trạng thái làm việc.";
        return RedirectToAction(nameof(StaffSchedule));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule([Bind(Prefix = "NewSchedule")] AdminCreateScheduleInput input)
    {
        if (!await IsOperationsAdminAsync()) return Forbid();
        var (name, start, end) = input.ShiftName switch {
            "Morning" => ("Ca Sáng", new TimeOnly(7, 0), new TimeOnly(12, 0)),
            "Afternoon" => ("Ca Chiều", new TimeOnly(12, 0), new TimeOnly(17, 0)),
            "Evening" => ("Ca Tối", new TimeOnly(17, 0), new TimeOnly(22, 0)),
            _ => ("", default(TimeOnly), default(TimeOnly))
        };
        if (input.WorkDate == DateOnly.MinValue) ModelState.AddModelError("NewSchedule.WorkDate", "Hãy chọn ngày làm việc.");
        if (ModelState.IsValid)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await _context.Users.AnyAsync(x => x.UserId == input.StaffId && x.IsActive && OperationsStaffRoles.Contains(x.Role.RoleName)))
                ModelState.AddModelError("NewSchedule.StaffId", "Nhân viên không tồn tại hoặc đã ngừng làm việc.");
            if (await _context.WorkSchedules.AnyAsync(x => x.StaffId == input.StaffId && x.WorkDate == input.WorkDate && x.StartTime < end && x.EndTime > start))
                ModelState.AddModelError("NewSchedule.ShiftName", "Nhân viên đã có lịch trùng khung giờ.");
            if (ModelState.IsValid)
            {
                var schedule = new WorkSchedule { StaffId = input.StaffId, WorkDate = input.WorkDate, ShiftName = name,
                    StartTime = start, EndTime = end, Note = input.Note,
                    CreatedByAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) };
                // A placeholder is Pending, never Present until attendance is recorded.
                schedule.Attendances.Add(new Attendance { StaffId = input.StaffId, Status = "Pending" });
                _context.WorkSchedules.Add(schedule);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["OperationsSuccess"] = "Đã phân ca và tạo bản ghi chấm công chờ cập nhật.";
                return RedirectToAction(nameof(StaffSchedule));
            }
        }
        return View("StaffSchedule", await LoadStaffScheduleAsync(new() { NewSchedule = input }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttendance([Bind(Prefix = "AttendanceEdit")] AdminAttendanceInput input)
    {
        if (!await IsOperationsAdminAsync()) return Forbid();
        var attendance = await _context.Attendances.Include(x => x.Schedule).SingleOrDefaultAsync(x => x.AttendanceId == input.AttendanceId);
        if (attendance == null) return NotFound();
        if (input.Status is "Present" or "Late" && !input.CheckInTime.HasValue)
            ModelState.AddModelError("", "Có mặt/đi muộn cần giờ vào.");
        if (input.CheckOutTime.HasValue && (!input.CheckInTime.HasValue || input.CheckOutTime <= input.CheckInTime))
            ModelState.AddModelError("", "Giờ ra phải sau giờ vào.");
        if (input.Status is "Absent" or "Leave" && (input.CheckInTime.HasValue || input.CheckOutTime.HasValue))
            ModelState.AddModelError("", "Vắng/nghỉ phép không được có giờ vào/ra.");
        if ((input.CheckInTime.HasValue && DateOnly.FromDateTime(input.CheckInTime.Value) != attendance.Schedule.WorkDate)
            || (input.CheckOutTime.HasValue && DateOnly.FromDateTime(input.CheckOutTime.Value) != attendance.Schedule.WorkDate))
            ModelState.AddModelError("", "Giờ chấm công phải thuộc ngày được phân ca.");
        if (!ModelState.IsValid)
            return View("StaffSchedule", await LoadStaffScheduleAsync(new() { AttendanceEdit = input }));
        attendance.CheckInTime = input.CheckInTime;
        attendance.CheckOutTime = input.CheckOutTime;
        attendance.Status = input.Status;
        attendance.Note = input.Note;
        await _context.SaveChangesAsync();
        TempData["OperationsSuccess"] = "Đã cập nhật chấm công.";
        return RedirectToAction(nameof(StaffSchedule));
    }
}
