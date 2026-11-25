
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record DepartmentDto(
    int DepartmentId,
    string DepartmentName,
    string? Description,
    bool IsActive
);
