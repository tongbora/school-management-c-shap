using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public string TeacherCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? SubjectSpecialty { get; set; }

    public int? DepartmentId { get; set; }

    public DateOnly HireDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual Department? Department { get; set; }
}
