
namespace SchoolManagement.Application.Students.DTOs;

public sealed record StudentDto(
    int StudentId,
    string StudentCode,
    string FullName,
    string Gender,
    DateOnly DateOfBirth,
    string? Phone,
    string? Email,
    string? Address,
    string Status,
    string? PhotoPath
);
