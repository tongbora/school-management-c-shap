
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class SubjectService : ISubjectService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public SubjectService(IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<SubjectDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Subjects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();

            query = query.Where(x =>
                x.SubjectCode.Contains(keyword) ||
                x.SubjectName.Contains(keyword));
        }

        return await query
            .OrderBy(x => x.SubjectName)
            .Select(x => new SubjectDto(
                x.SubjectId,
                x.SubjectCode,
                x.SubjectName,
                x.GradeLevel,
                x.IsActive))
            .ToListAsync();
    }

    public async Task<SubjectDto?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Subjects
            .AsNoTracking()
            .Where(x => x.SubjectId == id)
            .Select(x => new SubjectDto(
                x.SubjectId,
                x.SubjectCode,
                x.SubjectName,
                x.GradeLevel,
                x.IsActive))
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(
        SaveSubjectRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        string code = request.SubjectCode.Trim();

        if (await db.Subjects.AnyAsync(x => x.SubjectCode == code))
            throw new InvalidOperationException(
                "Subject code already exists.");

        var subject = new Subject
        {
            SubjectCode = code,
            SubjectName = request.SubjectName.Trim(),
            GradeLevel = Clean(request.GradeLevel),
            IsActive = true
        };

        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        return subject.SubjectId;
    }

    public async Task UpdateAsync(
        int id, SaveSubjectRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        var subject = await db.Subjects
            .FirstOrDefaultAsync(x => x.SubjectId == id);

        if (subject is null)
            throw new InvalidOperationException("Subject not found.");

        string code = request.SubjectCode.Trim();

        if (await db.Subjects.AnyAsync(x =>
            x.SubjectCode == code && x.SubjectId != id))
        {
            throw new InvalidOperationException(
                "Subject code already exists.");
        }

        subject.SubjectCode = code;
        subject.SubjectName = request.SubjectName.Trim();
        subject.GradeLevel = Clean(request.GradeLevel);

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var subject = await db.Subjects
            .FirstOrDefaultAsync(x => x.SubjectId == id);

        if (subject is null)
            throw new InvalidOperationException("Subject not found.");

        if (!isActive)
        {
            bool assignedToClasses = await db.ClassSubjects
                .AnyAsync(x => x.SubjectId == id);

            if (assignedToClasses)
            {
                throw new InvalidOperationException(
                    "This subject is assigned to classes. " +
                    "Review its assignments before deactivating.");
            }
        }

        subject.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    private static void Validate(SaveSubjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectCode))
            throw new ArgumentException("Subject code is required.");

        if (request.SubjectCode.Trim().Length > 50)
            throw new ArgumentException(
                "Subject code must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(request.SubjectName))
            throw new ArgumentException("Subject name is required.");

        if (request.SubjectName.Trim().Length > 120)
            throw new ArgumentException(
                "Subject name must not exceed 120 characters.");

        if (request.GradeLevel?.Trim().Length > 50)
            throw new ArgumentException(
                "Grade level must not exceed 50 characters.");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
