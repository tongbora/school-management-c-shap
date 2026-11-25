
namespace SchoolManagement.Application.Students.DTOs;

public sealed record SaveStudentRequest(
    string StudentCode,
    string FullName,
    string Gender,
    DateOnly DateOfBirth,
    string? Phone,
    string? Email,
    string? Address,
    string? PhotoPath
);
