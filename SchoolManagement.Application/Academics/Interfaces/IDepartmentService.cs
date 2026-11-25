
using SchoolManagement.Application.Academics.DTOs;

namespace SchoolManagement.Application.Academics.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(
        string? search = null);

    Task<DepartmentDto?> GetByIdAsync(int departmentId);

    Task<int> CreateAsync(SaveDepartmentRequest request);

    Task UpdateAsync(
        int departmentId,
        SaveDepartmentRequest request);

    Task SetActiveAsync(int departmentId, bool isActive);
}
