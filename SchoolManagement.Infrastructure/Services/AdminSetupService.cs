
using System.Data;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SchoolManagement.Application.Authentication.DTOs;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;
using SchoolManagement.Infrastructure.Security;

namespace SchoolManagement.Infrastructure.Services;

public sealed class AdminSetupService
{
    private readonly SchoolDbContext _db;
    private readonly PasswordService _passwordService;

    public AdminSetupService(
        SchoolDbContext db,
        PasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    public async Task<bool> IsSetupRequiredAsync()
    {
        return !await _db.Users.AnyAsync();
    }

    public async Task CreateAdminAsync(CreateAdminRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string fullName = request.FullName?.Trim() ?? "";
        string username = request.Username?.Trim() ?? "";
        string? email = request.Email?.Trim();

        // Validate full name
        if (string.IsNullOrWhiteSpace(fullName) ||
            fullName.Length > 150)
        {
            throw new ArgumentException("Invalid full name.");
        }

        // Validate username
        if (string.IsNullOrWhiteSpace(username) ||
            username.Length > 100)
        {
            throw new ArgumentException("Invalid username.");
        }

        // Validate email
        if (!string.IsNullOrWhiteSpace(email))
        {
            if (email.Length > 150 ||
                !MailAddress.TryCreate(email, out _))
            {
                throw new ArgumentException("Invalid email address.");
            }
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 12 ||
            request.Password.Length > 128)
        {
            throw new ArgumentException(
                "Password must contain 12 to 128 characters.");
        }

        // Begin database transaction
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        // Acquire SQL Server application lock
        var connection = _db.Database.GetDbConnection();

        using (var command = connection.CreateCommand())
        {
            command.Transaction =
                transaction.GetDbTransaction();

            command.CommandText = """
                DECLARE @result INT;

                EXEC @result = sys.sp_getapplock
                    @Resource = N'SchoolManagement.InitialAdminSetup',
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;

                SELECT @result;
                """;

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value ||
                Convert.ToInt32(result) < 0)
            {
                throw new InvalidOperationException(
                    "Administrator setup is currently unavailable.");
            }
        }

        // Only allow setup when there are no users
        if (await _db.Users.AnyAsync())
        {
            throw new InvalidOperationException(
                "Initial administrator setup is already complete.");
        }

        // Find Administrator role
        var administratorRole = await _db.Roles
            .SingleOrDefaultAsync(
                r => r.RoleName == "Administrator" &&
                     r.IsActive);

        if (administratorRole is null)
        {
            throw new InvalidOperationException(
                "Active Administrator role was not found.");
        }

        // Create user
        var user = new User
        {
            FullName = fullName,
            Username = username,
            Email = string.IsNullOrWhiteSpace(email)
                ? null
                : email,
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = string.Empty
        };

        // Securely hash password
        user.PasswordHash =
            _passwordService.HashPassword(
                user,
                request.Password);

        // Assign Administrator role
        user.Roles.Add(administratorRole);

        // Save user and role relationship
        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        // Commit transaction
        await transaction.CommitAsync();
    }
}
