
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record SaveAcademicYearRequest(
    string YearName,
    DateOnly StartDate,
    DateOnly EndDate
);
