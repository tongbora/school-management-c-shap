
namespace SchoolManagement.Application.Examinations.DTOs;

public sealed record BulkGradeRowDto(
    int EnrollmentId,
    string StudentCode,
    string StudentName,
    decimal? Score,
    decimal? MaxScore,
    string? Remark
);
