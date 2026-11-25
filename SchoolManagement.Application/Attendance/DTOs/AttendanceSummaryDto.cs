
namespace SchoolManagement.Application.Attendance.DTOs;

public sealed record AttendanceSummaryDto(
    int Total,
    int Present,
    int Absent,
    int Late,
    int Excused
);
