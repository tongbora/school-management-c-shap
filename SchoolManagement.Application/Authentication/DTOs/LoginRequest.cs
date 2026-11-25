using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManagement.Application.Authentication.DTOs;

public sealed record LoginRequest(
    string Username,
    string Password
);
