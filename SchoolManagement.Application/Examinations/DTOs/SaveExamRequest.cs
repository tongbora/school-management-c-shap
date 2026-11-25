
namespace SchoolManagement.Application.Examinations.DTOs;

public sealed record SaveExamRequest(
    string ExamName,
    string? ExamType,
    int AcademicYearId,
    DateOnly? StartDate,
    DateOnly? EndDate
);
