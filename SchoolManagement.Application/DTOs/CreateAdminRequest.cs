using System;
using System.Collections.Generic;
using System.Text;


namespace SchoolManagement.Application.Authentication.DTOs;

public sealed record CreateAdminRequest(
    string FullName,
    string Username,
    string? Email,
    string Password
);
