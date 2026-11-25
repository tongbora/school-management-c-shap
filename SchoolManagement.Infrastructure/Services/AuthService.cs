using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Authentication.DTOs;
using SchoolManagement.Application.Authentication.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;
using SchoolManagement.Infrastructure.Security;

namespace SchoolManagement.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly IDbContextFactory<SchoolDbContext> _dbFactory;
    private readonly PasswordService _passwordService;

    public AuthService(
        IDbContextFactory<SchoolDbContext> dbFactory,
        PasswordService passwordService)
    {
        _dbFactory = dbFactory;
        _passwordService = passwordService;
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var failure = new LoginResult(
            false,
            "Invalid username or password.",
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<string>());

        if (request is null ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return failure;
        }

        await using var db = await _dbFactory
            .CreateDbContextAsync(cancellationToken);

        var username = request.Username.Trim();

        var user = await db.Users
            .Include(u => u.Roles)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                u => u.Username == username,
                cancellationToken);

        if (user is null)
            return failure;

        if (!_passwordService.VerifyPassword(
                user, request.Password))
            return failure;

        if (!user.IsActive)
            return failure;

        if (user.MustChangePassword)
        {
            return new LoginResult(
                false,
                "Password change is required. Contact an administrator.",
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        var activeRoles = user.Roles
            .Where(r => r.IsActive)
            .ToList();

        if (activeRoles.Count == 0)
            return failure;

        var roleIds = activeRoles
            .Select(r => r.RoleId)
            .ToArray();

        var permissionKeys = await db.Roles
            .Where(r => roleIds.Contains(r.RoleId))
            .SelectMany(r => r.Permissions)
            .Select(p => p.PermissionKey)
            .Distinct()
            .ToListAsync(cancellationToken);

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            true,
            null,
            user.UserId,
            user.FullName,
            activeRoles.Select(r => r.RoleName).ToArray(),
            permissionKeys
        );
    }
}
