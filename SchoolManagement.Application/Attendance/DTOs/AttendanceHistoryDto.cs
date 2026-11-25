
namespace SchoolManagement.Application.Attendance.DTOs;

public sealed record AttendanceHistoryDto(
    int AttendanceId,
    int EnrollmentId,
    string StudentCode,
    string StudentName,
    string ClassName,
    DateOnly AttendanceDate,
    string SessionName,
    string Status,
    string? Remark,
    int? RecordedBy
);
