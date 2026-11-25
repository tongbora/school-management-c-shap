
using SchoolManagement.Application.Academics.DTOs;

namespace SchoolManagement.Application.Academics.Interfaces;

public interface IClassService
{
    Task<IReadOnlyList<ClassDto>> GetAllAsync(
        string? search = null);

    Task<ClassDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(SaveClassRequest request);

    Task UpdateAsync(int id, SaveClassRequest request);

    Task SetActiveAsync(int id, bool active);

    Task<IReadOnlyList<AcademicOptionDto>> GetYearsAsync();

    Task<IReadOnlyList<AcademicOptionDto>> GetRoomsAsync();

    Task<IReadOnlyList<AcademicOptionDto>> GetTeachersAsync();
}
