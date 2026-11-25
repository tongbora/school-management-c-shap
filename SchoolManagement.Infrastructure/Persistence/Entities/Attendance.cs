using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int EnrollmentId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public string SessionName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Remark { get; set; }

    public int? RecordedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual User? RecordedByNavigation { get; set; }
}
