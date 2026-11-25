
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record SaveSubjectRequest(
    string SubjectCode,
    string SubjectName,
    string? GradeLevel
);
