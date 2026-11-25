
using System.Data;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Attendance.DTOs;
using SchoolManagement.Application.Attendance.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using AttendanceEntity =
    SchoolManagement.Infrastructure.Persistence.Entities.Attendance;

namespace SchoolManagement.Infrastructure.Services;

public sealed class AttendanceService : IAttendanceService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    private static readonly string[] ValidStatuses =
    {
        "Present", "Absent", "Late", "Excused"
    };

    private static readonly string[] ValidSessions =
    {
        "FullDay", "Morning", "Afternoon"
    };

    public AttendanceService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<AttendanceOptionDto>>
        GetClassesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Classes
            .AsNoTracking()
            .OrderByDescending(x => x.AcademicYear.StartDate)
            .ThenBy(x => x.ClassName)
            .Select(x => new AttendanceOptionDto(
                x.ClassId,
                x.ClassName + " (" +
                x.AcademicYear.YearName + ")"))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AttendanceEntryDto>>
        GetRosterAsync(
            int classId,
            DateOnly date,
            string session)
    {
        ValidateSession(session);

        await using var db = await _factory.CreateDbContextAsync();

        await ValidateClassDateAsync(db, classId, date);

        return await RosterQuery(db, classId, date)
            .OrderBy(x => x.Student.FullName)
            .Select(x => new AttendanceEntryDto(
                x.EnrollmentId,
                x.Student.StudentCode,
                x.Student.FullName,
                db.Attendances
                    .Where(a =>
                        a.EnrollmentId == x.EnrollmentId &&
                        a.AttendanceDate == date &&
                        a.SessionName == session)
                    .Select(a => a.Status)
                    .FirstOrDefault(),
                db.Attendances
                    .Where(a =>
                        a.EnrollmentId == x.EnrollmentId &&
                        a.AttendanceDate == date &&
                        a.SessionName == session)
                    .Select(a => a.Remark)
                    .FirstOrDefault()))
            .ToListAsync();
    }

    public async Task SaveBatchAsync(
        SaveAttendanceBatchRequest request)
    {
        ValidateSession(request.SessionName);

        if (request.Marks is null || request.Marks.Count == 0)
            throw new ArgumentException(
                "No attendance records were provided.");

        if (request.Marks
            .GroupBy(x => x.EnrollmentId)
            .Any(g => g.Count() > 1))
        {
            throw new ArgumentException(
                "Duplicate enrollment IDs in attendance request.");
        }

        foreach (var mark in request.Marks)
        {
            if (!ValidStatuses.Contains(mark.Status))
                throw new ArgumentException(
                    $"Invalid attendance status: {mark.Status}");

            if (mark.Remark?.Trim().Length > 255)
                throw new ArgumentException(
                    "Attendance remarks cannot exceed 255 characters.");
        }

        await using var initialDb =
            await _factory.CreateDbContextAsync();

        var strategy = initialDb.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var db =
                await _factory.CreateDbContextAsync();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await ValidateClassDateAsync(
                db, request.ClassId, request.AttendanceDate);

            if (request.RecordedBy.HasValue &&
                !await db.Users.AnyAsync(
                    x => x.UserId == request.RecordedBy.Value &&
                         x.IsActive))
            {
                throw new InvalidOperationException(
                    "Recording user is invalid or inactive.");
            }

            var enrollmentIds = await RosterQuery(
                    db, request.ClassId, request.AttendanceDate)
                .Select(x => x.EnrollmentId)
                .ToListAsync();

            var submittedIds = request.Marks
                .Select(x => x.EnrollmentId)
                .ToHashSet();

            if (enrollmentIds.Count == 0)
                throw new InvalidOperationException(
                    "No enrolled students for this class and date.");

            if (enrollmentIds.Count != submittedIds.Count ||
                enrollmentIds.Any(id => !submittedIds.Contains(id)))
            {
                throw new InvalidOperationException(
                    "Attendance roster has changed. " +
                    "Reload the students and try again.");
            }

            var existing = await db.Attendances
                .Where(x =>
                    enrollmentIds.Contains(x.EnrollmentId) &&
                    x.AttendanceDate == request.AttendanceDate &&
                    x.SessionName == request.SessionName)
                .ToDictionaryAsync(x => x.EnrollmentId);

            foreach (var mark in request.Marks)
            {
                if (existing.TryGetValue(
                    mark.EnrollmentId, out var attendance))
                {
                    attendance.Status = mark.Status;
                    attendance.Remark = Clean(mark.Remark);
                    attendance.RecordedBy = request.RecordedBy;
                }
                else
                {
                    db.Attendances.Add(new AttendanceEntity
                    {
                        EnrollmentId = mark.EnrollmentId,
                        AttendanceDate = request.AttendanceDate,
                        SessionName = request.SessionName,
                        Status = mark.Status,
                        Remark = Clean(mark.Remark),
                        RecordedBy = request.RecordedBy,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<IReadOnlyList<AttendanceHistoryDto>>
        GetHistoryAsync(string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Attendances.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(x =>
                x.Enrollment.Student.FullName.Contains(keyword) ||
                x.Enrollment.Student.StudentCode.Contains(keyword) ||
                x.Enrollment.Class.ClassName.Contains(keyword) ||
                x.Status.Contains(keyword));
        }

        return await query
            .OrderByDescending(x => x.AttendanceDate)
            .ThenByDescending(x => x.AttendanceId)
            .Select(x => new AttendanceHistoryDto(
                x.AttendanceId,
                x.EnrollmentId,
                x.Enrollment.Student.StudentCode,
                x.Enrollment.Student.FullName,
                x.Enrollment.Class.ClassName,
                x.AttendanceDate,
                x.SessionName,
                x.Status,
                x.Remark,
                x.RecordedBy))
            .Take(500)
            .ToListAsync();
    }

    public async Task<AttendanceSummaryDto> GetSummaryAsync(
        int classId,
        DateOnly date,
        string session)
    {
        ValidateSession(session);

        await using var db = await _factory.CreateDbContextAsync();

        var rosterIds = RosterQuery(db, classId, date)
            .Select(x => x.EnrollmentId);

        var statuses = await db.Attendances
            .AsNoTracking()
            .Where(x =>
                rosterIds.Contains(x.EnrollmentId) &&
                x.AttendanceDate == date &&
                x.SessionName == session)
            .Select(x => x.Status)
            .ToListAsync();

        return new AttendanceSummaryDto(
            statuses.Count,
            statuses.Count(x => x == "Present"),
            statuses.Count(x => x == "Absent"),
            statuses.Count(x => x == "Late"),
            statuses.Count(x => x == "Excused"));
    }

    private static IQueryable<
        SchoolManagement.Infrastructure.Persistence.Entities.Enrollment>
        RosterQuery(
            SchoolDbContext db,
            int classId,
            DateOnly date)
    {
        return db.Enrollments.AsNoTracking()
            .Where(x =>
                x.ClassId == classId &&
                x.EnrollmentDate <= date &&
                (
                    x.EndDate == null ||
                    (x.Status == "Transferred"
                        ? date < x.EndDate.Value
                        : date <= x.EndDate.Value)
                ));
    }

    private static async Task ValidateClassDateAsync(
        SchoolDbContext db,
        int classId,
        DateOnly date)
    {
        var cls = await db.Classes
            .AsNoTracking()
            .Where(x => x.ClassId == classId)
            .Select(x => new
            {
                x.AcademicYear.StartDate,
                x.AcademicYear.EndDate
            })
            .FirstOrDefaultAsync();

        if (cls is null)
            throw new InvalidOperationException(
                "Class not found.");

        if (date < cls.StartDate || date > cls.EndDate)
            throw new ArgumentException(
                "Attendance date must be within " +
                "the class academic year.");
    }

    private static void ValidateSession(string session)
    {
        if (!ValidSessions.Contains(session))
            throw new ArgumentException(
                "Session must be FullDay, Morning, or Afternoon.");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
