
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class ClassSubjectService : IClassSubjectService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public ClassSubjectService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<ClassSubjectDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.ClassSubjects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(x =>
                x.Class.ClassName.Contains(keyword) ||
                x.Subject.SubjectName.Contains(keyword) ||
                x.Subject.SubjectCode.Contains(keyword) ||
                (x.Teacher != null &&
                 x.Teacher.FullName.Contains(keyword)) ||
                x.Class.AcademicYear.YearName.Contains(keyword));
        }

        return await query
            .OrderByDescending(x => x.Class.AcademicYear.StartDate)
            .ThenBy(x => x.Class.ClassName)
            .ThenBy(x => x.Subject.SubjectName)
            .Select(x => new ClassSubjectDto(
                x.ClassSubjectId,
                x.ClassId,
                x.Class.ClassName,
                x.AcademicYearId,
                x.Class.AcademicYear.YearName,
                x.SubjectId,
                x.Subject.SubjectName,
                x.Subject.SubjectCode,
                x.TeacherId,
                x.Teacher == null ? null : x.Teacher.FullName,
                x.CreatedAt))
            .ToListAsync();
    }

    public async Task<ClassSubjectDto?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.ClassSubjects
            .AsNoTracking()
            .Where(x => x.ClassSubjectId == id)
            .Select(x => new ClassSubjectDto(
                x.ClassSubjectId,
                x.ClassId,
                x.Class.ClassName,
                x.AcademicYearId,
                x.Class.AcademicYear.YearName,
                x.SubjectId,
                x.Subject.SubjectName,
                x.Subject.SubjectCode,
                x.TeacherId,
                x.Teacher == null ? null : x.Teacher.FullName,
                x.CreatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<AcademicOptionDto>>
        GetClassesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Classes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.AcademicYear.StartDate)
            .ThenBy(x => x.ClassName)
            .Select(x => new AcademicOptionDto(
                x.ClassId,
                x.ClassName + " (" +
                x.AcademicYear.YearName + ")"))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AcademicOptionDto>>
        GetSubjectsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Subjects
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SubjectName)
            .Select(x => new AcademicOptionDto(
                x.SubjectId,
                x.SubjectName + " (" + x.SubjectCode + ")"))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AcademicOptionDto>>
        GetTeachersAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Teachers
            .AsNoTracking()
            .Where(x => x.Status == "Active")
            .OrderBy(x => x.FullName)
            .Select(x => new AcademicOptionDto(
                x.TeacherId,
                x.FullName + " (" + x.TeacherCode + ")"))
            .ToListAsync();
    }

    public async Task<int> CreateAsync(
        SaveClassSubjectRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        int academicYearId = await ValidateReferencesAsync(
            db, request);

        bool duplicate = await db.ClassSubjects.AnyAsync(x =>
            x.ClassId == request.ClassId &&
            x.SubjectId == request.SubjectId &&
            x.AcademicYearId == academicYearId);

        if (duplicate)
            throw new InvalidOperationException(
                "This subject is already assigned to the class.");

        var entity = new ClassSubject
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            TeacherId = request.TeacherId,
            AcademicYearId = academicYearId,
            CreatedAt = DateTime.UtcNow
        };

        db.ClassSubjects.Add(entity);

        await db.SaveChangesAsync();

        return entity.ClassSubjectId;
    }

    public async Task UpdateAsync(
        int id, SaveClassSubjectRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var entity = await db.ClassSubjects
            .FirstOrDefaultAsync(x => x.ClassSubjectId == id);

        if (entity is null)
            throw new InvalidOperationException(
                "Class subject assignment not found.");

        // Existing grades or timetable slots depend on
        // the identity of the class-subject assignment.
        bool changingAssignment =
            entity.ClassId != request.ClassId ||
            entity.SubjectId != request.SubjectId;

        if (changingAssignment)
        {
            bool hasGrades = await db.Grades.AnyAsync(
                x => x.ClassSubjectId == id);

            bool hasTimetables = await db.Timetables.AnyAsync(
                x => x.ClassSubjectId == id);

            if (hasGrades || hasTimetables)
            {
                throw new InvalidOperationException(
                    "Cannot change the class or subject while " +
                    "grades or timetable records reference it. " +
                    "You may still change the assigned teacher.");
            }
        }

        int academicYearId = await ValidateReferencesAsync(
            db, request);

        bool duplicate = await db.ClassSubjects.AnyAsync(x =>
            x.ClassSubjectId != id &&
            x.ClassId == request.ClassId &&
            x.SubjectId == request.SubjectId &&
            x.AcademicYearId == academicYearId);

        if (duplicate)
            throw new InvalidOperationException(
                "This subject is already assigned to the class.");

        entity.ClassId = request.ClassId;
        entity.SubjectId = request.SubjectId;
        entity.TeacherId = request.TeacherId;
        entity.AcademicYearId = academicYearId;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var entity = await db.ClassSubjects
            .FirstOrDefaultAsync(x => x.ClassSubjectId == id);

        if (entity is null)
            throw new InvalidOperationException(
                "Assignment not found.");

        bool hasGrades = await db.Grades
            .AnyAsync(x => x.ClassSubjectId == id);

        bool hasTimetables = await db.Timetables
            .AnyAsync(x => x.ClassSubjectId == id);

        if (hasGrades || hasTimetables)
        {
            throw new InvalidOperationException(
                "This assignment is used by grades or timetables. " +
                "It cannot be deleted.");
        }

        db.ClassSubjects.Remove(entity);

        await db.SaveChangesAsync();
    }

    private static async Task<int> ValidateReferencesAsync(
        SchoolDbContext db,
        SaveClassSubjectRequest request)
    {
        if (request.ClassId <= 0 || request.SubjectId <= 0)
            throw new ArgumentException(
                "Select a class and subject.");

        var selectedClass = await db.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClassId == request.ClassId);

        if (selectedClass is null || !selectedClass.IsActive)
            throw new ArgumentException(
                "Selected class does not exist or is inactive.");

        bool subjectAvailable = await db.Subjects.AnyAsync(x =>
            x.SubjectId == request.SubjectId && x.IsActive);

        if (!subjectAvailable)
            throw new ArgumentException(
                "Selected subject does not exist or is inactive.");

        if (request.TeacherId.HasValue)
        {
            bool teacherAvailable = await db.Teachers.AnyAsync(x =>
                x.TeacherId == request.TeacherId.Value &&
                x.Status == "Active");

            if (!teacherAvailable)
                throw new ArgumentException(
                    "Selected teacher does not exist or is inactive.");
        }

        return selectedClass.AcademicYearId;
    }
}
