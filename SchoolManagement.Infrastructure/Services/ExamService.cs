
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Examinations.DTOs;
using SchoolManagement.Application.Examinations.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class ExamService : IExamService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public ExamService(IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<ExamDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Exams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(x =>
                x.ExamName.Contains(keyword) ||
                (x.ExamType != null &&
                 x.ExamType.Contains(keyword)) ||
                x.AcademicYear.YearName.Contains(keyword));
        }

        return await query
            .OrderByDescending(x => x.AcademicYear.StartDate)
            .ThenBy(x => x.ExamName)
            .Select(x => new ExamDto(
                x.ExamId,
                x.ExamName,
                x.ExamType,
                x.AcademicYearId,
                x.AcademicYear.YearName,
                x.StartDate,
                x.EndDate,
                x.Status))
            .ToListAsync();
    }

    public async Task<ExamDto?> GetByIdAsync(int examId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Exams
            .AsNoTracking()
            .Where(x => x.ExamId == examId)
            .Select(x => new ExamDto(
                x.ExamId,
                x.ExamName,
                x.ExamType,
                x.AcademicYearId,
                x.AcademicYear.YearName,
                x.StartDate,
                x.EndDate,
                x.Status))
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(SaveExamRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        await ValidateYearAsync(db, request.AcademicYearId);

        var exam = new Exam
        {
            ExamName = request.ExamName.Trim(),
            ExamType = Clean(request.ExamType),
            AcademicYearId = request.AcademicYearId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };

        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        return exam.ExamId;
    }

    public async Task UpdateAsync(
        int examId,
        SaveExamRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        var exam = await db.Exams.FindAsync(examId)
            ?? throw new InvalidOperationException(
                "Examination not found.");

        if (exam.Status != "Draft")
            throw new InvalidOperationException(
                "Only Draft examinations can be edited.");

        if (await db.Grades.AnyAsync(x => x.ExamId == examId))
            throw new InvalidOperationException(
                "Cannot edit an examination with grades.");

        await ValidateYearAsync(db, request.AcademicYearId);

        exam.ExamName = request.ExamName.Trim();
        exam.ExamType = Clean(request.ExamType);
        exam.AcademicYearId = request.AcademicYearId;
        exam.StartDate = request.StartDate;
        exam.EndDate = request.EndDate;

        await db.SaveChangesAsync();
    }

    public async Task SetStatusAsync(int examId, string status)
    {
        if (status != "Open" && status != "Closed")
            throw new ArgumentException(
                "Status must be Open or Closed.");

        await using var db = await _factory.CreateDbContextAsync();

        var exam = await db.Exams.FindAsync(examId)
            ?? throw new InvalidOperationException(
                "Examination not found.");

        bool allowed =
            (exam.Status == "Draft" && status == "Open") ||
            (exam.Status == "Open" && status == "Closed");

        if (!allowed)
            throw new InvalidOperationException(
                $"Invalid exam status transition: " +
                $"{exam.Status} to {status}.");

        exam.Status = status;

        await db.SaveChangesAsync();
    }

    private static async Task ValidateYearAsync(
        SchoolDbContext db, int academicYearId)
    {
        bool exists = await db.AcademicYears
            .AnyAsync(x => x.AcademicYearId == academicYearId);

        if (!exists)
            throw new ArgumentException(
                "Selected academic year does not exist.");
    }

    private static void Validate(SaveExamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExamName) ||
            request.ExamName.Trim().Length > 120)
        {
            throw new ArgumentException(
                "Exam name is required (maximum 120 characters).");
        }

        if (request.ExamType?.Trim().Length > 50)
            throw new ArgumentException(
                "Exam type cannot exceed 50 characters.");

        if (request.AcademicYearId <= 0)
            throw new ArgumentException(
                "Select an academic year.");

        if (request.StartDate.HasValue &&
            request.EndDate.HasValue &&
            request.EndDate < request.StartDate)
        {
            throw new ArgumentException(
                "End date cannot be before start date.");
        }
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
