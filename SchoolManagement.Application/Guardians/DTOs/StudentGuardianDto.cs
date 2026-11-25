
namespace SchoolManagement.Application.Guardians.DTOs;

public sealed record StudentGuardianDto(
    int GuardianId,
    string FullName,
    string Relationship,
    bool IsPrimary,
    string? Phone,
    string? Email,
    string? Occupation
);
