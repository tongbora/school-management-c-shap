
namespace SchoolManagement.Application.Attendance.DTOs;

public sealed record AttendanceMarkDto(
    int EnrollmentId,
    string Status,
    string? Remark
);
