
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class ClassService : IClassService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public ClassService(IDbContextFactory<SchoolDbContext> factory)
        => _factory = factory;

    public async Task<IReadOnlyList<ClassDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Classes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where(x =>
                x.ClassName.Contains(keyword) ||
                x.GradeLevel.Contains(keyword));
        }

        return await query
            .OrderByDescending(x => x.AcademicYear.StartDate)
            .ThenBy(x => x.ClassName)
            .Select(x => new ClassDto(
                x.ClassId,
                x.ClassName,
                x.GradeLevel,
                x.AcademicYearId,
                x.AcademicYear.YearName,
                x.RoomId,
                x.Room == null ? null : x.Room.RoomCode,
                x.HomeroomTeacherId,
                x.HomeroomTeacher == null
                    ? null
                    : x.HomeroomTeacher.FullName,
                x.Capacity,
                x.IsActive))
            .ToListAsync();
    }

    public async Task<ClassDto?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Classes.AsNoTracking()
            .Where(x => x.ClassId == id)
            .Select(x => new ClassDto(
                x.ClassId,
                x.ClassName,
                x.GradeLevel,
                x.AcademicYearId,
                x.AcademicYear.YearName,
                x.RoomId,
                x.Room == null ? null : x.Room.RoomCode,
                x.HomeroomTeacherId,
                x.HomeroomTeacher == null
                    ? null
                    : x.HomeroomTeacher.FullName,
                x.Capacity,
                x.IsActive))
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<AcademicOptionDto>> GetYearsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.AcademicYears.AsNoTracking()
            .OrderByDescending(x => x.StartDate)
            .Select(x => new AcademicOptionDto(
                x.AcademicYearId, x.YearName))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AcademicOptionDto>> GetRoomsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Rooms.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.RoomCode)
            .Select(x => new AcademicOptionDto(
                x.RoomId, x.RoomCode))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AcademicOptionDto>> GetTeachersAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Teachers.AsNoTracking()
            .Where(x => x.Status == "Active")
            .OrderBy(x => x.FullName)
            .Select(x => new AcademicOptionDto(
                x.TeacherId, x.FullName + " (" + x.TeacherCode + ")"))
            .ToListAsync();
    }

    public async Task<int> CreateAsync(SaveClassRequest request)
    {
        Validate(request);
        await using var db = await _factory.CreateDbContextAsync();

        await ValidateReferencesAsync(db, request);

        string name = request.ClassName.Trim();

        if (await db.Classes.AnyAsync(x =>
            x.ClassName == name &&
            x.AcademicYearId == request.AcademicYearId))
            throw new InvalidOperationException(
                "Class name already exists in this academic year.");

        var entity = new Class
        {
            ClassName = name,
            GradeLevel = request.GradeLevel.Trim(),
            AcademicYearId = request.AcademicYearId,
            RoomId = request.RoomId,
            HomeroomTeacherId = request.HomeroomTeacherId,
            Capacity = request.Capacity,
            IsActive = true
        };

        db.Classes.Add(entity);
        await db.SaveChangesAsync();
        return entity.ClassId;
    }

    public async Task UpdateAsync(int id, SaveClassRequest request)
    {
        Validate(request);
        await using var db = await _factory.CreateDbContextAsync();

        var entity = await db.Classes.FindAsync(id)
            ?? throw new InvalidOperationException("Class not found.");

        await ValidateReferencesAsync(db, request);

        string name = request.ClassName.Trim();

        if (await db.Classes.AnyAsync(x =>
            x.ClassId != id &&
            x.AcademicYearId == request.AcademicYearId &&
            x.ClassName == name))
            throw new InvalidOperationException(
                "Class name already exists in this academic year.");

        if (entity.AcademicYearId != request.AcademicYearId &&
            (await db.Enrollments.AnyAsync(x => x.ClassId == id) ||
             await db.ClassSubjects.AnyAsync(x => x.ClassId == id)))
            throw new InvalidOperationException(
                "Cannot change academic year after enrollment " +
                "or subject assignments exist.");

        entity.ClassName = name;
        entity.GradeLevel = request.GradeLevel.Trim();
        entity.AcademicYearId = request.AcademicYearId;
        entity.RoomId = request.RoomId;
        entity.HomeroomTeacherId = request.HomeroomTeacherId;
        entity.Capacity = request.Capacity;

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var entity = await db.Classes.FindAsync(id)
            ?? throw new InvalidOperationException("Class not found.");

        if (!active && await db.Enrollments.AnyAsync(x =>
            x.ClassId == id && x.Status == "Active"))
            throw new InvalidOperationException(
                "Cannot deactivate a class with active enrollments.");

        entity.IsActive = active;
        await db.SaveChangesAsync();
    }

    private static async Task ValidateReferencesAsync(
        SchoolDbContext db, SaveClassRequest request)
    {
        if (!await db.AcademicYears.AnyAsync(
            x => x.AcademicYearId == request.AcademicYearId))
            throw new ArgumentException("Invalid academic year.");

        if (request.RoomId.HasValue)
        {
            var room = await db.Rooms.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.RoomId == request.RoomId.Value);

            if (room == null || !room.IsActive)
                throw new ArgumentException(
                    "Selected room does not exist or is inactive.");

            if (request.Capacity > room.Capacity)
                throw new ArgumentException(
                    "Class capacity exceeds room capacity.");
        }

        if (request.HomeroomTeacherId.HasValue &&
            !await db.Teachers.AnyAsync(x =>
                x.TeacherId == request.HomeroomTeacherId.Value &&
                x.Status == "Active"))
            throw new ArgumentException(
                "Selected homeroom teacher is unavailable.");
    }

    private static void Validate(SaveClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClassName) ||
            request.ClassName.Trim().Length > 100)
            throw new ArgumentException(
                "Class name is required (maximum 100 characters).");

        if (string.IsNullOrWhiteSpace(request.GradeLevel) ||
            request.GradeLevel.Trim().Length > 50)
            throw new ArgumentException(
                "Grade level is required (maximum 50 characters).");

        if (request.AcademicYearId <= 0)
            throw new ArgumentException("Choose an academic year.");

        if (request.Capacity <= 0)
            throw new ArgumentException(
                "Class capacity must be greater than zero.");
    }
}
