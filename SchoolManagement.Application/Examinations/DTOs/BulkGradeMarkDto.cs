
namespace SchoolManagement.Application.Examinations.DTOs;

public sealed record BulkGradeMarkDto(
    int EnrollmentId,
    decimal? Score,
    string? Remark
);
