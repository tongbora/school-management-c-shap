
namespace SchoolManagement.Application.Attendance.DTOs;

public sealed record AttendanceEntryDto(
    int EnrollmentId,
    string StudentCode,
    string StudentName,
    string? Status,
    string? Remark
);
