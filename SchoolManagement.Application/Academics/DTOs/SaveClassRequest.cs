
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record SaveClassRequest(
    string ClassName,
    string GradeLevel,
    int AcademicYearId,
    int? RoomId,
    int? HomeroomTeacherId,
    int Capacity
);
