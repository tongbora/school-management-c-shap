
namespace SchoolManagement.Application.Guardians.DTOs;

public sealed record GuardianDto(
    int GuardianId,
    string FullName,
    string? Gender,
    string? Phone,
    string? Email,
    string? Occupation,
    string? Address
);
