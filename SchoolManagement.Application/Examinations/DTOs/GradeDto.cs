
namespace SchoolManagement.Application.Examinations.DTOs;

public sealed record GradeDto(
    int GradeId,
    int EnrollmentId,
    string StudentName,
    int ClassSubjectId,
    string ClassName,
    string SubjectName,
    int ExamId,
    string ExamName,
    int AcademicYearId,
    decimal Score,
    decimal MaxScore,
    decimal Percentage,
    string? Remark
);
