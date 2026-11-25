
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record ClassDto(
    int ClassId,
    string ClassName,
    string GradeLevel,
    int AcademicYearId,
    string AcademicYearName,
    int? RoomId,
    string? RoomName,
    int? HomeroomTeacherId,
    string? HomeroomTeacherName,
    int Capacity,
    bool IsActive
);
