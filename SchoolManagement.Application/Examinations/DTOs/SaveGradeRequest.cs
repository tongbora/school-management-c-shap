
namespace SchoolManagement.Application.Examinations.DTOs;

public sealed record SaveGradeRequest(
    int EnrollmentId,
    int ClassSubjectId,
    int ExamId,
    decimal Score,
    decimal MaxScore,
    string? Remark
);
