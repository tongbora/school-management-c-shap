using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManagement.Application.Authentication.DTOs;

public sealed record LoginResult(
    bool IsSuccess,
    string? ErrorMessage,
    int? UserId,
    string? FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
