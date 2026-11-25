
using SchoolManagement.Application.Attendance.DTOs;

namespace SchoolManagement.Application.Attendance.Interfaces;

public interface IAttendanceService
{
    Task<IReadOnlyList<AttendanceOptionDto>>
        GetClassesAsync();

    Task<IReadOnlyList<AttendanceEntryDto>>
        GetRosterAsync(
            int classId,
            DateOnly date,
            string session);

    Task SaveBatchAsync(
        SaveAttendanceBatchRequest request);

    Task<IReadOnlyList<AttendanceHistoryDto>>
        GetHistoryAsync(string? search = null);

    Task<AttendanceSummaryDto> GetSummaryAsync(
        int classId,
        DateOnly date,
        string session);
}
