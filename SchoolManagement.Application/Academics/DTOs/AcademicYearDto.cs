
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record AcademicYearDto(
    int AcademicYearId,
    string YearName,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive
);
