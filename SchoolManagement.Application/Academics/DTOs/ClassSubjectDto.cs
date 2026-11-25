
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record ClassSubjectDto(
    int ClassSubjectId,
    int ClassId,
    string ClassName,
    int AcademicYearId,
    string AcademicYearName,
    int SubjectId,
    string SubjectName,
    string SubjectCode,
    int? TeacherId,
    string? TeacherName,
    DateTime CreatedAt
);
