
namespace SchoolManagement.Application.Guardians.DTOs;

public sealed record SaveGuardianRequest(
    string FullName,
    string? Gender,
    string? Phone,
    string? Email,
    string? Occupation,
    string? Address
);
