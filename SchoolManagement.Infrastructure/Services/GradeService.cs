
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using SchoolManagement.Application.Examinations.DTOs;
using SchoolManagement.Application.Examinations.Interfaces;

using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class GradeService : IGradeService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public GradeService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    // =========================================
    // 1. Get all grades
    // =========================================

    public async Task<IReadOnlyList<GradeDto>> GetAllAsync(
        string? search = null)
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        var query = db.Grades.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(x =>
                x.Enrollment.Student.FullName.Contains(keyword) ||
                x.Enrollment.Student.StudentCode.Contains(keyword) ||
                x.Enrollment.Class.ClassName.Contains(keyword) ||
                x.ClassSubject.Subject.SubjectName.Contains(keyword) ||
                x.Exam.ExamName.Contains(keyword));
        }

        var grades = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.GradeId,
                x.EnrollmentId,

                StudentName =
                    x.Enrollment.Student.FullName,

                x.ClassSubjectId,

                ClassName =
                    x.Enrollment.Class.ClassName,

                SubjectName =
                    x.ClassSubject.Subject.SubjectName,

                x.ExamId,

                ExamName =
                    x.Exam.ExamName,

                x.AcademicYearId,
                x.Score,
                x.MaxScore,
                x.Remark
            })
            .Take(500)
            .ToListAsync();

        return grades
            .Select(x => new GradeDto(
                x.GradeId,
                x.EnrollmentId,
                x.StudentName,
                x.ClassSubjectId,
                x.ClassName,
                x.SubjectName,
                x.ExamId,
                x.ExamName,
                x.AcademicYearId,
                x.Score,
                x.MaxScore,
                x.MaxScore <= 0
                    ? 0
                    : Math.Round(
                        x.Score / x.MaxScore * 100m,
                        2),
                x.Remark
            ))
            .ToList();
    }

    // =========================================
    // 2. Get open examinations
    // =========================================

    public async Task<IReadOnlyList<GradeOptionDto>>
        GetOpenExamsAsync()
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        return await db.Exams
            .AsNoTracking()
            .Where(x => x.Status == "Open")
            .OrderByDescending(
                x => x.AcademicYear.StartDate)
            .ThenBy(x => x.ExamName)
            .Select(x => new GradeOptionDto(
                x.ExamId,
                x.ExamName + " (" +
                x.AcademicYear.YearName + ")"
            ))
            .ToListAsync();
    }

    // =========================================
    // 3. Get class subjects for an examination
    // =========================================

    public async Task<IReadOnlyList<GradeOptionDto>>
        GetClassSubjectsAsync(int examId)
    {
        if (examId <= 0)
            return [];

        await using var db =
            await _factory.CreateDbContextAsync();

        var exam = await db.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ExamId == examId);

        if (exam is null || exam.Status != "Open")
            return [];

        return await db.ClassSubjects
            .AsNoTracking()
            .Where(x =>
                x.AcademicYearId == exam.AcademicYearId &&
                x.Class.IsActive &&
                x.Subject.IsActive)
            .OrderBy(x => x.Class.ClassName)
            .ThenBy(x => x.Subject.SubjectName)
            .Select(x => new GradeOptionDto(
                x.ClassSubjectId,
                x.Class.ClassName + " - " +
                x.Subject.SubjectName + " (" +
                x.Subject.SubjectCode + ")"
            ))
            .ToListAsync();
    }

    // =========================================
    // 4. Get enrolled students for class subject
    // =========================================

    public async Task<IReadOnlyList<GradeOptionDto>>
        GetEnrollmentsAsync(int classSubjectId)
    {
        if (classSubjectId <= 0)
            return [];

        await using var db =
            await _factory.CreateDbContextAsync();

        var assignment = await db.ClassSubjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ClassSubjectId == classSubjectId);

        if (assignment is null)
            return [];

        return await db.Enrollments
            .AsNoTracking()
            .Where(x =>
                x.ClassId == assignment.ClassId &&
                x.AcademicYearId ==
                    assignment.AcademicYearId &&
                x.Status == "Active" &&
                x.Student.Status == "Active")
            .OrderBy(x => x.Student.FullName)
            .Select(x => new GradeOptionDto(
                x.EnrollmentId,
                x.Student.FullName + " (" +
                x.Student.StudentCode + ")"
            ))
            .ToListAsync();
    }

    // =========================================
    // 5. Create or update student grade
    // =========================================

    public async Task<int> SaveAsync(
        SaveGradeRequest request,
        int? recordedBy = null)
    {
        ValidateScore(request);

        await using var initialDb =
            await _factory.CreateDbContextAsync();

        var strategy =
            initialDb.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var db =
                await _factory.CreateDbContextAsync();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // =====================================
            // Validate examination
            // =====================================

            var exam = await db.Exams
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ExamId == request.ExamId);

            if (exam is null)
            {
                throw new InvalidOperationException(
                    "Examination not found.");
            }

            if (exam.Status != "Open")
            {
                throw new InvalidOperationException(
                    "The examination must be Open " +
                    "before grades can be recorded.");
            }

            // =====================================
            // Validate enrollment
            // =====================================

            var enrollment = await db.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EnrollmentId ==
                        request.EnrollmentId);

            if (enrollment is null)
            {
                throw new InvalidOperationException(
                    "Student enrollment not found.");
            }

            // =====================================
            // Validate class subject
            // =====================================

            var classSubject = await db.ClassSubjects
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ClassSubjectId ==
                        request.ClassSubjectId);

            if (classSubject is null)
            {
                throw new InvalidOperationException(
                    "Class subject assignment not found.");
            }

            // =====================================
            // Validate academic year and class
            // =====================================

            if (enrollment.AcademicYearId !=
                    exam.AcademicYearId ||
                classSubject.AcademicYearId !=
                    exam.AcademicYearId ||
                enrollment.ClassId !=
                    classSubject.ClassId)
            {
                throw new InvalidOperationException(
                    "The enrollment, class subject, " +
                    "and examination must belong to " +
                    "the same class and academic year.");
            }

            // =====================================
            // Validate enrollment status
            // =====================================

            if (enrollment.Status == "Withdrawn")
            {
                throw new InvalidOperationException(
                    "Cannot record a grade for " +
                    "a withdrawn enrollment.");
            }

            // =====================================
            // Validate recording user
            // =====================================

            if (recordedBy.HasValue)
            {
                bool validUser = await db.Users
                    .AnyAsync(x =>
                        x.UserId == recordedBy.Value &&
                        x.IsActive);

                if (!validUser)
                {
                    throw new InvalidOperationException(
                        "Recording user is invalid " +
                        "or inactive.");
                }
            }

            // =====================================
            // Find existing grade
            // =====================================

            var grade = await db.Grades
                .FirstOrDefaultAsync(x =>
                    x.EnrollmentId ==
                        request.EnrollmentId &&
                    x.ClassSubjectId ==
                        request.ClassSubjectId &&
                    x.ExamId ==
                        request.ExamId);

            int gradeId;

            if (grade is null)
            {
                // =================================
                // Insert new grade
                // =================================

                grade = new Grade
                {
                    EnrollmentId =
                        request.EnrollmentId,

                    ClassSubjectId =
                        request.ClassSubjectId,

                    ExamId =
                        request.ExamId,

                    AcademicYearId =
                        exam.AcademicYearId,

                    Score =
                        request.Score,

                    MaxScore =
                        request.MaxScore,

                    Remark =
                        Clean(request.Remark),

                    RecordedBy =
                        recordedBy,

                    CreatedAt =
                        DateTime.UtcNow
                };

                db.Grades.Add(grade);
            }
            else
            {
                // =================================
                // Update existing grade
                // =================================

                grade.Score =
                    request.Score;

                grade.MaxScore =
                    request.MaxScore;

                grade.Remark =
                    Clean(request.Remark);

                // Preserve the previous recorder when
                // no authenticated user ID is supplied.
                if (recordedBy.HasValue)
                {
                    grade.RecordedBy = recordedBy;
                }

                grade.UpdatedAt =
                    DateTime.UtcNow;
            }

            // =====================================
            // Save and commit transaction
            // =====================================

            await db.SaveChangesAsync();

            gradeId = grade.GradeId;

            await transaction.CommitAsync();

            return gradeId;
        });
    }

    // =========================================
    // 6. Validate score
    // =========================================

    private static void ValidateScore(
        SaveGradeRequest request)
    {
        if (request.EnrollmentId <= 0 ||
            request.ClassSubjectId <= 0 ||
            request.ExamId <= 0)
        {
            throw new ArgumentException(
                "Enrollment, subject, and " +
                "examination are required.");
        }

        if (request.MaxScore <= 0)
        {
            throw new ArgumentException(
                "Maximum score must be greater than zero.");
        }

        if (request.Score < 0)
        {
            throw new ArgumentException(
                "Score cannot be negative.");
        }

        if (request.Score > request.MaxScore)
        {
            throw new ArgumentException(
                "Score cannot exceed maximum score.");
        }

        // SQL Server DECIMAL(6,2)
        if (request.Score > 9999.99m ||
            request.MaxScore > 9999.99m)
        {
            throw new ArgumentException(
                "Score values cannot exceed 9999.99.");
        }

        if (decimal.Round(request.Score, 2) !=
                request.Score ||
            decimal.Round(request.MaxScore, 2) !=
                request.MaxScore)
        {
            throw new ArgumentException(
                "Score values support a maximum " +
                "of two decimal places.");
        }

        if (request.Remark?.Trim().Length > 255)
        {
            throw new ArgumentException(
                "Remark cannot exceed 255 characters.");
        }
    }

    // =========================================
    // 7. Clean optional text
    // =========================================

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }


    public async Task<IReadOnlyList<BulkGradeRowDto>>
        GetBulkRowsAsync(int examId, int classSubjectId)
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        var exam = await db.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExamId == examId);

        if (exam is null || exam.Status != "Open")
            throw new InvalidOperationException(
                "Select an open examination.");

        var assignment = await db.ClassSubjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ClassSubjectId == classSubjectId);

        if (assignment is null ||
            assignment.AcademicYearId != exam.AcademicYearId)
        {
            throw new InvalidOperationException(
                "The subject and examination must belong " +
                "to the same academic year.");
        }

        var roster = await db.Enrollments
            .AsNoTracking()
            .Where(x =>
                x.ClassId == assignment.ClassId &&
                x.AcademicYearId == exam.AcademicYearId &&
                x.Status == "Active" &&
                x.Student.Status == "Active")
            .OrderBy(x => x.Student.FullName)
            .Select(x => new
            {
                x.EnrollmentId,
                x.Student.StudentCode,
                x.Student.FullName
            })
            .ToListAsync();

        var ids = roster
            .Select(x => x.EnrollmentId)
            .ToList();

        var grades = await db.Grades
            .AsNoTracking()
            .Where(x =>
                x.ExamId == examId &&
                x.ClassSubjectId == classSubjectId &&
                ids.Contains(x.EnrollmentId))
            .ToDictionaryAsync(x => x.EnrollmentId);

        return roster.Select(x =>
        {
            grades.TryGetValue(x.EnrollmentId, out var grade);

            return new BulkGradeRowDto(
                x.EnrollmentId,
                x.StudentCode,
                x.FullName,
                grade?.Score,
                grade?.MaxScore,
                grade?.Remark);
        }).ToList();
    }

    public async Task<int> SaveBulkAsync(
        SaveBulkGradesRequest request,
        int? recordedBy = null)
    {
        if (request.ExamId <= 0 ||
            request.ClassSubjectId <= 0)
            throw new ArgumentException(
                "Select an examination and class subject.");

        if (request.MaxScore <= 0 ||
            request.MaxScore > 9999.99m ||
            decimal.Round(request.MaxScore, 2) !=
                request.MaxScore)
        {
            throw new ArgumentException(
                "Maximum score must be greater than zero, " +
                "at most 9999.99, and use two decimal places.");
        }

        if (request.Marks is null || request.Marks.Count == 0)
            throw new ArgumentException(
                "No student grades were provided.");

        if (request.Marks.Any(x => x.EnrollmentId <= 0))
            throw new ArgumentException(
                "Invalid enrollment ID.");

        if (request.Marks
            .GroupBy(x => x.EnrollmentId)
            .Any(x => x.Count() > 1))
        {
            throw new ArgumentException(
                "Duplicate enrollment IDs in the request.");
        }

        foreach (var mark in request.Marks)
        {
            if (mark.Score.HasValue)
            {
                decimal score = mark.Score.Value;

                if (score < 0 ||
                    score > request.MaxScore ||
                    decimal.Round(score, 2) != score)
                {
                    throw new ArgumentException(
                        "Every score must be between zero " +
                        "and the maximum score, with " +
                        "at most two decimal places.");
                }
            }

            if (mark.Remark?.Trim().Length > 255)
                throw new ArgumentException(
                    "Remark cannot exceed 255 characters.");
        }

        // Blank scores are skipped, not saved as zero.
        var scoredMarks = request.Marks
            .Where(x => x.Score.HasValue)
            .ToList();

        if (scoredMarks.Count == 0)
            throw new ArgumentException(
                "Enter at least one score.");

        await using var initialDb =
            await _factory.CreateDbContextAsync();

        var strategy =
            initialDb.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var db =
                await _factory.CreateDbContextAsync();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var exam = await db.Exams
                .FirstOrDefaultAsync(x =>
                    x.ExamId == request.ExamId);

            if (exam is null || exam.Status != "Open")
                throw new InvalidOperationException(
                    "Examination is not open for grading.");

            var assignment = await db.ClassSubjects
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ClassSubjectId == request.ClassSubjectId);

            if (assignment is null ||
                assignment.AcademicYearId != exam.AcademicYearId)
            {
                throw new InvalidOperationException(
                    "Invalid class subject for this examination.");
            }

            if (recordedBy.HasValue)
            {
                bool validUser = await db.Users.AnyAsync(x =>
                    x.UserId == recordedBy.Value &&
                    x.IsActive);

                if (!validUser)
                    throw new InvalidOperationException(
                        "Recording user is invalid or inactive.");
            }

            var requestedIds = scoredMarks
                .Select(x => x.EnrollmentId)
                .ToList();

            var validIds = await db.Enrollments
                .AsNoTracking()
                .Where(x =>
                    requestedIds.Contains(x.EnrollmentId) &&
                    x.ClassId == assignment.ClassId &&
                    x.AcademicYearId == exam.AcademicYearId &&
                    x.Status == "Active" &&
                    x.Student.Status == "Active")
                .Select(x => x.EnrollmentId)
                .ToListAsync();

            if (validIds.Count != requestedIds.Count)
                throw new InvalidOperationException(
                    "Some students are no longer actively " +
                    "enrolled in the selected class. " +
                    "Reload the roster.");

            var existing = await db.Grades
                .Where(x =>
                    x.ExamId == request.ExamId &&
                    x.ClassSubjectId == request.ClassSubjectId &&
                    requestedIds.Contains(x.EnrollmentId))
                .ToDictionaryAsync(x => x.EnrollmentId);

            var now = DateTime.UtcNow;

            foreach (var mark in scoredMarks)
            {
                if (existing.TryGetValue(
                    mark.EnrollmentId, out var grade))
                {
                    grade.Score = mark.Score!.Value;
                    grade.MaxScore = request.MaxScore;
                    grade.Remark = Clean(mark.Remark);
                    grade.UpdatedAt = now;

                    if (recordedBy.HasValue)
                        grade.RecordedBy = recordedBy;
                }
                else
                {
                    db.Grades.Add(new Grade
                    {
                        EnrollmentId = mark.EnrollmentId,
                        ClassSubjectId = request.ClassSubjectId,
                        ExamId = request.ExamId,
                        AcademicYearId = exam.AcademicYearId,
                        Score = mark.Score!.Value,
                        MaxScore = request.MaxScore,
                        Remark = Clean(mark.Remark),
                        RecordedBy = recordedBy,
                        CreatedAt = now
                    });
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return scoredMarks.Count;
        });
    }

}
