using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class ClassSubject
{
    public int ClassSubjectId { get; set; }

    public int ClassId { get; set; }

    public int SubjectId { get; set; }

    public int? TeacherId { get; set; }

    public int AcademicYearId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual Teacher? Teacher { get; set; }

    public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
}
