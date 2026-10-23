using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class WorkSchedule
{
    public int ScheduleId { get; set; }

    public int StaffId { get; set; }

    public int CreatedByAdminId { get; set; }

    public DateOnly WorkDate { get; set; }

    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Note { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual User CreatedByAdmin { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
