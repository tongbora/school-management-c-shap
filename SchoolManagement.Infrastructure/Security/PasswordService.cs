using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Security;

public sealed class PasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    // Hash a new password before saving it to SQL Server
    public string HashPassword(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    // Verify a password during login
    public bool VerifyPassword(User user, string password)
    {
        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password
        );

        return result != PasswordVerificationResult.Failed;
    }
}

