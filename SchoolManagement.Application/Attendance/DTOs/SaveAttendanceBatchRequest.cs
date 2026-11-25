
namespace SchoolManagement.Application.Attendance.DTOs;

public sealed record SaveAttendanceBatchRequest(
    int ClassId,
    DateOnly AttendanceDate,
    string SessionName,
    int? RecordedBy,
    IReadOnlyList<AttendanceMarkDto> Marks
);
