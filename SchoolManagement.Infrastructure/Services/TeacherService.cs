using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Teachers.DTOs;
using SchoolManagement.Application.Teachers.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class TeacherService : ITeacherService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public TeacherService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<TeacherDto>> GetTeachersAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Teachers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(x =>
                x.TeacherCode.Contains(keyword) ||
                x.FullName.Contains(keyword));
        }

        var teachers = await query
            .OrderBy(x => x.TeacherCode)
            .ToListAsync();

        return teachers.Select(Map).ToList();
    }

    public async Task<TeacherDto?> GetByIdAsync(int teacherId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var teacher = await db.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TeacherId == teacherId);

        return teacher is null ? null : Map(teacher);
    }

    public async Task<int> CreateAsync(
        SaveTeacherRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        string code = request.TeacherCode.Trim();

        bool exists = await db.Teachers
            .AnyAsync(x => x.TeacherCode == code);

        if (exists)
            throw new InvalidOperationException(
                "Teacher code already exists.");

        await ValidateDepartmentAsync(
            db, request.DepartmentId);

        var teacher = new Teacher
        {
            TeacherCode = code,
            FullName = request.FullName.Trim(),
            Gender = Clean(request.Gender),
            DateOfBirth = request.DateOfBirth,
            Phone = Clean(request.Phone),
            Email = Clean(request.Email),
            SubjectSpecialty = Clean(request.SubjectSpecialty),
            DepartmentId = request.DepartmentId,
            HireDate = request.HireDate,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        db.Teachers.Add(teacher);

        await db.SaveChangesAsync();

        return teacher.TeacherId;
    }

    public async Task UpdateAsync(
        int teacherId,
        SaveTeacherRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        var teacher = await db.Teachers
            .FirstOrDefaultAsync(x =>
                x.TeacherId == teacherId);

        if (teacher is null)
            throw new InvalidOperationException(
                "Teacher not found.");

        string code = request.TeacherCode.Trim();

        bool duplicate = await db.Teachers.AnyAsync(x =>
            x.TeacherCode == code &&
            x.TeacherId != teacherId);

        if (duplicate)
            throw new InvalidOperationException(
                "Teacher code already exists.");

        await ValidateDepartmentAsync(
            db, request.DepartmentId);

        teacher.TeacherCode = code;
        teacher.FullName = request.FullName.Trim();
        teacher.Gender = Clean(request.Gender);
        teacher.DateOfBirth = request.DateOfBirth;
        teacher.Phone = Clean(request.Phone);
        teacher.Email = Clean(request.Email);
        teacher.SubjectSpecialty =
            Clean(request.SubjectSpecialty);
        teacher.DepartmentId = request.DepartmentId;
        teacher.HireDate = request.HireDate;

        await db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int teacherId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var teacher = await db.Teachers
            .FirstOrDefaultAsync(x =>
                x.TeacherId == teacherId);

        if (teacher is null)
            throw new InvalidOperationException(
                "Teacher not found.");

        teacher.Status = "Inactive";

        await db.SaveChangesAsync();
    }

    private static async Task ValidateDepartmentAsync(
        SchoolDbContext db,
        int? departmentId)
    {
        if (!departmentId.HasValue)
            return;

        bool exists = await db.Departments
            .AnyAsync(x => x.DepartmentId == departmentId.Value);

        if (!exists)
            throw new InvalidOperationException(
                "Selected department does not exist.");
    }

    private static void Validate(
        SaveTeacherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TeacherCode))
            throw new ArgumentException(
                "Teacher code is required.");

        if (request.TeacherCode.Trim().Length > 50)
            throw new ArgumentException(
                "Teacher code cannot exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException(
                "Full name is required.");

        if (request.FullName.Trim().Length > 150)
            throw new ArgumentException(
                "Full name cannot exceed 150 characters.");

        CheckLength(request.Gender, 20, "Gender");
        CheckLength(request.Phone, 30, "Phone");
        CheckLength(request.Email, 150, "Email");
        CheckLength(
            request.SubjectSpecialty,
            120,
            "Subject specialty");

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !System.Net.Mail.MailAddress.TryCreate(
                request.Email.Trim(), out _))
        {
            throw new ArgumentException(
                "Invalid email address.");
        }

        if (request.HireDate > DateOnly.FromDateTime(
                DateTime.Today))
        {
            throw new ArgumentException(
                "Hire date cannot be in the future.");
        }

        if (request.DateOfBirth.HasValue &&
            request.DateOfBirth.Value >
                DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException(
                "Date of birth cannot be in the future.");
        }

        if (request.DepartmentId is <= 0)
            throw new ArgumentException(
                "Department ID must be positive.");
    }

    private static void CheckLength(
        string? value, int maxLength, string field)
    {
        if (value?.Trim().Length > maxLength)
            throw new ArgumentException(
                $"{field} cannot exceed {maxLength} characters.");
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static TeacherDto Map(Teacher x)
    {
        return new TeacherDto(
            x.TeacherId,
            x.TeacherCode,
            x.FullName,
            x.Gender,
            x.DateOfBirth,
            x.Phone,
            x.Email,
            x.SubjectSpecialty,
            x.DepartmentId,
            x.HireDate,
            x.Status
        );
    }


    public async Task<IReadOnlyList<DepartmentOptionDto>>
        GetDepartmentsAsync()
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        return await db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentOptionDto(
                d.DepartmentId,
                d.DepartmentName))
            .ToListAsync();
    }


}

