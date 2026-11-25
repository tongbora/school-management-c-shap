using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public int AcademicYearId { get; set; }

    public DateOnly EnrollmentDate { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly? EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Fee> Fees { get; set; } = new List<Fee>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual Student Student { get; set; } = null!;
}
