
namespace SchoolManagement.Application.Examinations.DTOs;

public sealed record ExamDto(
    int ExamId,
    string ExamName,
    string? ExamType,
    int AcademicYearId,
    string AcademicYearName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status
);
