using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Class
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public string GradeLevel { get; set; } = null!;

    public int AcademicYearId { get; set; }

    public int? RoomId { get; set; }

    public int? HomeroomTeacherId { get; set; }

    public int Capacity { get; set; }

    public bool IsActive { get; set; }

    public virtual AcademicYear AcademicYear { get; set; } = null!;

    public virtual ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual Teacher? HomeroomTeacher { get; set; }

    public virtual Room? Room { get; set; }
}
