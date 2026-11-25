
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Guardians.DTOs;
using SchoolManagement.Application.Guardians.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;
using System.Data;
using System.Net.Mail;

namespace SchoolManagement.Infrastructure.Services;

public sealed class GuardianService : IGuardianService
{
    private readonly IDbContextFactory<SchoolDbContext> _dbFactory;

    public GuardianService(
        IDbContextFactory<SchoolDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<GuardianDto>> SearchAsync(
        string? keyword = null)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync();

        var query = db.Guardians.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string search = keyword.Trim();

            query = query.Where(g =>
                g.FullName.Contains(search) ||
                (g.Phone != null && g.Phone.Contains(search)) ||
                (g.Email != null && g.Email.Contains(search)));
        }

        return await query
            .OrderBy(g => g.FullName)
            .Select(g => new GuardianDto(
                g.GuardianId,
                g.FullName,
                g.Gender,
                g.Phone,
                g.Email,
                g.Occupation,
                g.Address))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StudentGuardianDto>>
        GetStudentGuardiansAsync(int studentId)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync();

        return await db.StudentGuardians
            .AsNoTracking()
            .Where(sg => sg.StudentId == studentId)
            .OrderByDescending(sg => sg.IsPrimary)
            .ThenBy(sg => sg.Guardian.FullName)
            .Select(sg => new StudentGuardianDto(
                sg.GuardianId,
                sg.Guardian.FullName,
                sg.Relationship,
                sg.IsPrimary,
                sg.Guardian.Phone,
                sg.Guardian.Email,
                sg.Guardian.Occupation))
            .ToListAsync();
    }

    public async Task<int> CreateAsync(
        SaveGuardianRequest request)
    {
        Validate(request);

        await using var db =
            await _dbFactory.CreateDbContextAsync();

        var guardian = new Guardian
        {
            FullName = request.FullName.Trim(),
            Gender = Optional(request.Gender),
            Phone = Optional(request.Phone),
            Email = Optional(request.Email),
            Occupation = Optional(request.Occupation),
            Address = Optional(request.Address),
            CreatedAt = DateTime.UtcNow
        };

        db.Guardians.Add(guardian);
        await db.SaveChangesAsync();

        return guardian.GuardianId;
    }

    public async Task UpdateAsync(
        int guardianId,
        SaveGuardianRequest request)
    {
        Validate(request);

        await using var db =
            await _dbFactory.CreateDbContextAsync();

        var guardian = await db.Guardians
            .FindAsync(guardianId);

        if (guardian is null)
            throw new KeyNotFoundException(
                "Guardian not found.");

        guardian.FullName = request.FullName.Trim();
        guardian.Gender = Optional(request.Gender);
        guardian.Phone = Optional(request.Phone);
        guardian.Email = Optional(request.Email);
        guardian.Occupation = Optional(request.Occupation);
        guardian.Address = Optional(request.Address);

        await db.SaveChangesAsync();
    }

    public async Task LinkAsync(
        int studentId,
        int guardianId,
        string relationship,
        bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(relationship) ||
            relationship.Trim().Length > 50)
        {
            throw new ArgumentException(
                "Relationship must contain 1 to 50 characters.");
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        if (!await db.Students.AnyAsync(
            s => s.StudentId == studentId))
        {
            throw new KeyNotFoundException("Student not found.");
        }

        if (!await db.Guardians.AnyAsync(
            g => g.GuardianId == guardianId))
        {
            throw new KeyNotFoundException("Guardian not found.");
        }

        bool exists = await db.StudentGuardians
            .AnyAsync(sg =>
                sg.StudentId == studentId &&
                sg.GuardianId == guardianId);

        if (exists)
            throw new InvalidOperationException(
                "This guardian is already linked to the student.");

        if (isPrimary)
            await ClearPrimaryAsync(db, studentId);

        db.StudentGuardians.Add(new StudentGuardian
        {
            StudentId = studentId,
            GuardianId = guardianId,
            Relationship = relationship.Trim(),
            IsPrimary = isPrimary
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task SetPrimaryAsync(
        int studentId,
        int guardianId)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var link = await db.StudentGuardians
            .SingleOrDefaultAsync(sg =>
                sg.StudentId == studentId &&
                sg.GuardianId == guardianId);

        if (link is null)
            throw new KeyNotFoundException(
                "Student-guardian relationship not found.");

        await ClearPrimaryAsync(db, studentId);

        link.IsPrimary = true;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task UnlinkAsync(
        int studentId,
        int guardianId)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync();

        var link = await db.StudentGuardians
            .SingleOrDefaultAsync(sg =>
                sg.StudentId == studentId &&
                sg.GuardianId == guardianId);

        if (link is null)
            throw new KeyNotFoundException(
                "Student-guardian relationship not found.");

        db.StudentGuardians.Remove(link);
        await db.SaveChangesAsync();
    }

    private static async Task ClearPrimaryAsync(
        SchoolDbContext db,
        int studentId)
    {
        // Persist the reset before assigning the next primary.
        // This avoids violating the filtered unique index.
        await db.StudentGuardians
            .Where(sg =>
                sg.StudentId == studentId &&
                sg.IsPrimary)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    sg => sg.IsPrimary, false));
    }


    private static void Validate(SaveGuardianRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            request.FullName.Trim().Length > 150)
        {
            throw new ArgumentException(
                "Full name must contain 1 to 150 characters.");
        }

        if (request.Gender?.Trim().Length > 20)
            throw new ArgumentException(
                "Gender cannot exceed 20 characters.");

        if (request.Phone?.Trim().Length > 30)
            throw new ArgumentException(
                "Phone cannot exceed 30 characters.");

        if (request.Email?.Trim().Length > 150)
            throw new ArgumentException(
                "Email cannot exceed 150 characters.");

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !MailAddress.TryCreate(request.Email, out _))
        {
            throw new ArgumentException("Invalid email address.");
        }

        if (request.Occupation?.Trim().Length > 120)
            throw new ArgumentException(
                "Occupation cannot exceed 120 characters.");

        if (request.Address?.Trim().Length > 255)
            throw new ArgumentException(
                "Address cannot exceed 255 characters.");
    }


    private static string? Optional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
