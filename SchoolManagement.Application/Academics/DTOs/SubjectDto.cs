
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record SubjectDto(
    int SubjectId,
    string SubjectCode,
    string SubjectName,
    string? GradeLevel,
    bool IsActive
);
