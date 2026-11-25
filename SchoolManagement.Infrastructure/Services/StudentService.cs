
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Students.DTOs;
using SchoolManagement.Application.Students.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class StudentService : IStudentService
{
    private readonly IDbContextFactory<SchoolDbContext> _dbFactory;

    public StudentService(
        IDbContextFactory<SchoolDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<StudentDto>> GetStudentsAsync(
        string? search = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(s =>
                s.StudentCode.Contains(keyword) ||
                s.FullName.Contains(keyword));
        }

        return await query
            .OrderBy(s => s.FullName)
            .Select(s => new StudentDto(
                s.StudentId,
                s.StudentCode,
                s.FullName,
                s.Gender,
                s.DateOfBirth,
                s.Phone,
                s.Email,
                s.Address,
                s.Status,
                s.PhotoPath
            ))
            .ToListAsync();
    }

    public async Task<StudentDto?> GetByIdAsync(int studentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .Select(s => new StudentDto(
                s.StudentId,
                s.StudentCode,
                s.FullName,
                s.Gender,
                s.DateOfBirth,
                s.Phone,
                s.Email,
                s.Address,
                s.Status,
                s.PhotoPath
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(
        SaveStudentRequest request)
    {
        ValidateRequest(request);

        await using var db = await _dbFactory.CreateDbContextAsync();

        string studentCode = request.StudentCode.Trim();

        if (await db.Students.AnyAsync(
            s => s.StudentCode == studentCode))
        {
            throw new InvalidOperationException(
                "Student code already exists.");
        }

        var student = new Student
        {
            StudentCode = studentCode,
            FullName = request.FullName.Trim(),
            Gender = request.Gender.Trim(),
            DateOfBirth = request.DateOfBirth,
            Phone = NormalizeOptional(request.Phone),
            Email = NormalizeOptional(request.Email),
            Address = NormalizeOptional(request.Address),
            Status = "Active",
            PhotoPath = request.PhotoPath,
            CreatedAt = DateTime.UtcNow
        };

        db.Students.Add(student);

        await db.SaveChangesAsync();

        return student.StudentId;
    }

    public async Task UpdateAsync(
        int studentId,
        SaveStudentRequest request)
    {
        ValidateRequest(request);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var student = await db.Students.FindAsync(studentId);

        if (student is null)
            throw new KeyNotFoundException(
                "Student not found.");

        string studentCode = request.StudentCode.Trim();

        bool duplicate = await db.Students.AnyAsync(s =>
            s.StudentCode == studentCode &&
            s.StudentId != studentId);

        if (duplicate)
            throw new InvalidOperationException(
                "Student code already exists.");

        student.StudentCode = studentCode;
        student.FullName = request.FullName.Trim();
        student.Gender = request.Gender.Trim();
        student.DateOfBirth = request.DateOfBirth;
        student.Phone = NormalizeOptional(request.Phone);
        student.Email = NormalizeOptional(request.Email);
        student.Address = NormalizeOptional(request.Address);
        student.PhotoPath = request.PhotoPath; 
        student.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int studentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var student = await db.Students.FindAsync(studentId);

        if (student is null)
            throw new KeyNotFoundException(
                "Student not found.");

        student.Status = "Inactive";
        student.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private static void ValidateRequest(
        SaveStudentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.StudentCode) ||
            request.StudentCode.Trim().Length > 50)
            throw new ArgumentException(
                "Student code is required and must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            request.FullName.Trim().Length > 150)
            throw new ArgumentException(
                "Full name is required and must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.Gender) ||
            request.Gender.Trim().Length > 20)
            throw new ArgumentException("Invalid gender.");

        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException(
                "Date of birth cannot be in the future.");

        if (request.Phone?.Trim().Length > 30)
            throw new ArgumentException("Phone number is too long.");

        if (request.Email?.Trim().Length > 150)
            throw new ArgumentException("Email is too long.");

        if (request.Address?.Trim().Length > 255)
            throw new ArgumentException("Address is too long.");

        if (request.PhotoPath?.Length > 500)
        {
            throw new ArgumentException(
                "Photo path cannot exceed 500 characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
