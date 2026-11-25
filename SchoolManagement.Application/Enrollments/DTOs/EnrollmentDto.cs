
namespace SchoolManagement.Application.Enrollments.DTOs;

public sealed record EnrollmentDto(
    int EnrollmentId,
    int StudentId,
    string StudentCode,
    string StudentName,
    int ClassId,
    string ClassName,
    int AcademicYearId,
    string AcademicYearName,
    DateOnly EnrollmentDate,
    string Status,
    DateOnly? EndDate
);
