using System;
using System.Collections.Generic;
using System.Text;

using SchoolManagement.Application.Authentication.DTOs;

namespace SchoolManagement.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
