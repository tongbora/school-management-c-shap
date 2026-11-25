
using System.Data;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Enrollments.DTOs;
using SchoolManagement.Application.Enrollments.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class EnrollmentService : IEnrollmentService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public EnrollmentService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Enrollments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(x =>
                x.Student.StudentCode.Contains(keyword) ||
                x.Student.FullName.Contains(keyword) ||
                x.Class.ClassName.Contains(keyword) ||
                x.Class.AcademicYear.YearName.Contains(keyword));
        }

        return await query
            .OrderByDescending(x => x.EnrollmentDate)
            .ThenByDescending(x => x.EnrollmentId)
            .Select(x => new EnrollmentDto(
                x.EnrollmentId,
                x.StudentId,
                x.Student.StudentCode,
                x.Student.FullName,
                x.ClassId,
                x.Class.ClassName,
                x.AcademicYearId,
                x.Class.AcademicYear.YearName,
                x.EnrollmentDate,
                x.Status,
                x.EndDate))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<EnrollmentOptionDto>>
        GetStudentsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .Where(x => x.Status == "Active")
            .OrderBy(x => x.FullName)
            .Select(x => new EnrollmentOptionDto(
                x.StudentId,
                x.FullName + " (" + x.StudentCode + ")"))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<EnrollmentOptionDto>>
        GetClassesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Classes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.AcademicYear.StartDate)
            .ThenBy(x => x.ClassName)
            .Select(x => new EnrollmentOptionDto(
                x.ClassId,
                x.ClassName + " (" +
                x.AcademicYear.YearName + ")"))
            .ToListAsync();
    }

    public async Task<int> CreateAsync(
        CreateEnrollmentRequest request)
    {
        if (request.StudentId <= 0 || request.ClassId <= 0)
            throw new ArgumentException(
                "Select a student and class.");

        await using var db = await _factory.CreateDbContextAsync();

        // Supports transactions with EF Core's retry strategy.
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context =
                await _factory.CreateDbContextAsync();

            await using var transaction =
                await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var student = await context.Students
                .FirstOrDefaultAsync(x =>
                    x.StudentId == request.StudentId);

            if (student is null || student.Status != "Active")
                throw new InvalidOperationException(
                    "Student does not exist or is inactive.");

            var cls = await context.Classes
                .Include(x => x.AcademicYear)
                .FirstOrDefaultAsync(x =>
                    x.ClassId == request.ClassId);

            if (cls is null || !cls.IsActive)
                throw new InvalidOperationException(
                    "Class does not exist or is inactive.");

            ValidateDate(
                request.EnrollmentDate, cls.AcademicYear);

            bool alreadyActive = await context.Enrollments
                .AnyAsync(x =>
                    x.StudentId == request.StudentId &&
                    x.AcademicYearId == cls.AcademicYearId &&
                    x.Status == "Active");

            if (alreadyActive)
                throw new InvalidOperationException(
                    "Student already has an active enrollment " +
                    "in this academic year.");

            bool duplicate = await context.Enrollments
                .AnyAsync(x =>
                    x.StudentId == request.StudentId &&
                    x.ClassId == cls.ClassId &&
                    x.AcademicYearId == cls.AcademicYearId);

            if (duplicate)
                throw new InvalidOperationException(
                    "Student already has an enrollment record " +
                    "for this class and academic year.");

            await ValidateCapacityAsync(context, cls);

            var enrollment = new Enrollment
            {
                StudentId = request.StudentId,
                ClassId = cls.ClassId,
                AcademicYearId = cls.AcademicYearId,
                EnrollmentDate = request.EnrollmentDate,
                Status = "Active",
                EndDate = null,
                CreatedAt = DateTime.UtcNow
            };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return enrollment.EnrollmentId;
        });
    }

    public async Task<int> TransferAsync(
        TransferEnrollmentRequest request)
    {
        if (request.EnrollmentId <= 0 ||
            request.TargetClassId <= 0)
            throw new ArgumentException(
                "Select an enrollment and target class.");

        await using var db = await _factory.CreateDbContextAsync();
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context =
                await _factory.CreateDbContextAsync();

            await using var transaction =
                await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var source = await context.Enrollments
                .FirstOrDefaultAsync(x =>
                    x.EnrollmentId == request.EnrollmentId);

            if (source is null || source.Status != "Active")
                throw new InvalidOperationException(
                    "Only active enrollments can be transferred.");

            if (request.TransferDate < source.EnrollmentDate)
                throw new InvalidOperationException(
                    "Transfer date cannot be before enrollment date.");

            if (source.ClassId == request.TargetClassId)
                throw new InvalidOperationException(
                    "Target class must be different.");

            var target = await context.Classes
                .Include(x => x.AcademicYear)
                .FirstOrDefaultAsync(x =>
                    x.ClassId == request.TargetClassId);

            if (target is null || !target.IsActive)
                throw new InvalidOperationException(
                    "Target class is unavailable.");

            if (target.AcademicYearId != source.AcademicYearId)
                throw new InvalidOperationException(
                    "Transfers must stay within the same academic year.");

            ValidateDate(
                request.TransferDate, target.AcademicYear);

            bool destinationExists =
                await context.Enrollments.AnyAsync(x =>
                    x.StudentId == source.StudentId &&
                    x.ClassId == target.ClassId &&
                    x.AcademicYearId == target.AcademicYearId);

            if (destinationExists)
                throw new InvalidOperationException(
                    "An enrollment record already exists for " +
                    "this target class and academic year.");

            await ValidateCapacityAsync(context, target);

            source.Status = "Transferred";
            source.EndDate = request.TransferDate;

            // Flush old status before inserting the new active
            // enrollment, satisfying the filtered unique index.
            await context.SaveChangesAsync();

            var destination = new Enrollment
            {
                StudentId = source.StudentId,
                ClassId = target.ClassId,
                AcademicYearId = target.AcademicYearId,
                EnrollmentDate = request.TransferDate,
                Status = "Active",
                EndDate = null,
                CreatedAt = DateTime.UtcNow
            };

            context.Enrollments.Add(destination);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return destination.EnrollmentId;
        });
    }

    public async Task SetStatusAsync(
        int enrollmentId,
        string status,
        DateOnly endDate)
    {
        if (status != "Completed" && status != "Withdrawn")
            throw new ArgumentException(
                "Status must be Completed or Withdrawn.");

        await using var db = await _factory.CreateDbContextAsync();

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(x =>
                x.EnrollmentId == enrollmentId);

        if (enrollment is null)
            throw new InvalidOperationException(
                "Enrollment not found.");

        if (enrollment.Status != "Active")
            throw new InvalidOperationException(
                "Only active enrollments can be closed.");

        if (endDate < enrollment.EnrollmentDate)
            throw new ArgumentException(
                "End date cannot be before enrollment date.");

        enrollment.Status = status;
        enrollment.EndDate = endDate;

        await db.SaveChangesAsync();
    }

    private static async Task ValidateCapacityAsync(
        SchoolDbContext db,
        Class cls)
    {
        int enrolled = await db.Enrollments.CountAsync(x =>
            x.ClassId == cls.ClassId &&
            x.Status == "Active");

        if (enrolled >= cls.Capacity)
            throw new InvalidOperationException(
                $"Class is full ({enrolled}/{cls.Capacity}).");

        if (cls.RoomId.HasValue)
        {
            var room = await db.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.RoomId == cls.RoomId.Value);

            if (room is null || !room.IsActive)
                throw new InvalidOperationException(
                    "The assigned room is unavailable.");

            if (cls.Capacity > room.Capacity)
                throw new InvalidOperationException(
                    "Class capacity exceeds room capacity.");
        }
    }

    private static void ValidateDate(
        DateOnly date,
        AcademicYear year)
    {
        if (date < year.StartDate || date > year.EndDate)
            throw new ArgumentException(
                $"Date must be between {year.StartDate} " +
                $"and {year.EndDate}.");
    }
}
