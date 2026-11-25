using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Grade
{
    public int GradeId { get; set; }

    public int EnrollmentId { get; set; }

    public int ClassSubjectId { get; set; }

    public int ExamId { get; set; }

    public int AcademicYearId { get; set; }

    public decimal Score { get; set; }

    public decimal MaxScore { get; set; }

    public string? Remark { get; set; }

    public int? RecordedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ClassSubject ClassSubject { get; set; } = null!;

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual Exam Exam { get; set; } = null!;

    public virtual User? RecordedByNavigation { get; set; }
}
