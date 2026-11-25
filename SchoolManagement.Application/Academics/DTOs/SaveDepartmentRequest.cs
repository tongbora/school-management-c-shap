
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record SaveDepartmentRequest(
    string DepartmentName,
    string? Description
);
