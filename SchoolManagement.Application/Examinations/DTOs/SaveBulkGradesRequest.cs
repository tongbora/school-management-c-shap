
namespace SchoolManagement.Application.Examinations.DTOs;

public sealed record SaveBulkGradesRequest(
    int ExamId,
    int ClassSubjectId,
    decimal MaxScore,
    IReadOnlyList<BulkGradeMarkDto> Marks
);
