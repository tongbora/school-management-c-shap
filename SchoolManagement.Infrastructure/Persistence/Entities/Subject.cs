using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Subject
{
    public int SubjectId { get; set; }

    public string SubjectCode { get; set; } = null!;

    public string SubjectName { get; set; } = null!;

    public string? GradeLevel { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
}
