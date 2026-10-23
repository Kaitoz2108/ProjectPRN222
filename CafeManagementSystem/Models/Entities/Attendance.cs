using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int ScheduleId { get; set; }

    public int StaffId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public virtual WorkSchedule Schedule { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
