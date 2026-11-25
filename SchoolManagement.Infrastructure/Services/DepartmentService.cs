
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class DepartmentService : IDepartmentService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public DepartmentService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();

            query = query.Where(x =>
                x.DepartmentName.Contains(keyword) ||
                (x.Description != null &&
                 x.Description.Contains(keyword)));
        }

        var departments = await query
            .OrderBy(x => x.DepartmentName)
            .Select(x => new DepartmentDto(
                x.DepartmentId,
                x.DepartmentName,
                x.Description,
                x.IsActive))
            .ToListAsync();

        return departments;
    }

    public async Task<DepartmentDto?> GetByIdAsync(
        int departmentId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Departments
            .AsNoTracking()
            .Where(x => x.DepartmentId == departmentId)
            .Select(x => new DepartmentDto(
                x.DepartmentId,
                x.DepartmentName,
                x.Description,
                x.IsActive))
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(
        SaveDepartmentRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        string name = request.DepartmentName.Trim();

        bool exists = await db.Departments
            .AnyAsync(x => x.DepartmentName == name);

        if (exists)
            throw new InvalidOperationException(
                "Department name already exists.");

        var department = new Department
        {
            DepartmentName = name,
            Description = Clean(request.Description),
            IsActive = true
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return department.DepartmentId;
    }

    public async Task UpdateAsync(
        int departmentId,
        SaveDepartmentRequest request)
    {
        Validate(request);

        await using var db = await _factory.CreateDbContextAsync();

        var department = await db.Departments
            .FirstOrDefaultAsync(x =>
                x.DepartmentId == departmentId);

        if (department is null)
            throw new InvalidOperationException(
                "Department not found.");

        string name = request.DepartmentName.Trim();

        bool duplicate = await db.Departments.AnyAsync(x =>
            x.DepartmentName == name &&
            x.DepartmentId != departmentId);

        if (duplicate)
            throw new InvalidOperationException(
                "Department name already exists.");

        department.DepartmentName = name;
        department.Description = Clean(request.Description);

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(
        int departmentId,
        bool isActive)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var department = await db.Departments
            .FirstOrDefaultAsync(x =>
                x.DepartmentId == departmentId);

        if (department is null)
            throw new InvalidOperationException(
                "Department not found.");

        if (!isActive)
        {
            bool assignedToTeachers = await db.Teachers
                .AnyAsync(x =>
                    x.DepartmentId == departmentId &&
                    x.Status == "Active");

            if (assignedToTeachers)
            {
                throw new InvalidOperationException(
                    "Cannot deactivate a department " +
                    "while active teachers are assigned to it.");
            }
        }

        department.IsActive = isActive;

        await db.SaveChangesAsync();
    }

    private static void Validate(SaveDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentName))
            throw new ArgumentException(
                "Department name is required.");

        if (request.DepartmentName.Trim().Length > 120)
            throw new ArgumentException(
                "Department name cannot exceed 120 characters.");

        if (request.Description?.Trim().Length > 500)
            throw new ArgumentException(
                "Description cannot exceed 500 characters.");
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
