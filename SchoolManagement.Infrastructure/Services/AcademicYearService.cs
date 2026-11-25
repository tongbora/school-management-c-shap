
using System.Data;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class AcademicYearService : IAcademicYearService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public AcademicYearService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<AcademicYearDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.AcademicYears.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            query = query.Where(x => x.YearName.Contains(keyword));
        }

        var records = await query
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        return records.Select(Map).ToList();
    }

    public async Task<AcademicYearDto?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var year = await db.AcademicYears
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AcademicYearId == id);

        return year is null ? null : Map(year);
    }

    public async Task<int> CreateAsync(
        SaveAcademicYearRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        string name = request.YearName.Trim();

        if (await db.AcademicYears.AnyAsync(
                x => x.YearName == name))
            throw new InvalidOperationException(
                "Academic year name already exists.");

        var year = new AcademicYear
        {
            YearName = name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        db.AcademicYears.Add(year);
        await db.SaveChangesAsync();

        return year.AcademicYearId;
    }

    public async Task UpdateAsync(
        int id, SaveAcademicYearRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        var year = await db.AcademicYears
            .FirstOrDefaultAsync(x => x.AcademicYearId == id);

        if (year is null)
            throw new InvalidOperationException(
                "Academic year not found.");

        // Do not silently change an academic period
        // after classes or examinations have been linked.
        bool hasClasses = await db.Classes
            .AnyAsync(x => x.AcademicYearId == id);

        bool hasExams = await db.Exams
            .AnyAsync(x => x.AcademicYearId == id);

        if (hasClasses || hasExams)
        {
            if (year.StartDate != request.StartDate ||
                year.EndDate != request.EndDate)
                throw new InvalidOperationException(
                    "Cannot change dates for an academic year " +
                    "already used by classes or examinations.");
        }

        string name = request.YearName.Trim();

        bool duplicate = await db.AcademicYears.AnyAsync(
            x => x.YearName == name && x.AcademicYearId != id);

        if (duplicate)
            throw new InvalidOperationException(
                "Academic year name already exists.");

        year.YearName = name;
        year.StartDate = request.StartDate;
        year.EndDate = request.EndDate;

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        bool exists = await db.AcademicYears
            .AnyAsync(x => x.AcademicYearId == id);

        if (!exists)
            throw new InvalidOperationException(
                "Academic year not found.");

        // Two separate statements avoid violating the
        // filtered unique index on IsActive = 1.
        await db.AcademicYears
            .Where(x => x.IsActive)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(x => x.IsActive, false));

        await db.AcademicYears
            .Where(x => x.AcademicYearId == id)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(x => x.IsActive, true));

        await transaction.CommitAsync();
    }

    private static void Validate(SaveAcademicYearRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.YearName))
            throw new ArgumentException(
                "Academic year name is required.");

        if (request.YearName.Trim().Length > 50)
            throw new ArgumentException(
                "Academic year name cannot exceed 50 characters.");

        if (request.EndDate < request.StartDate)
            throw new ArgumentException(
                "End date must be after or equal to start date.");
    }

    private static AcademicYearDto Map(AcademicYear x)
    {
        return new AcademicYearDto(
            x.AcademicYearId,
            x.YearName,
            x.StartDate,
            x.EndDate,
            x.IsActive);
    }
}
