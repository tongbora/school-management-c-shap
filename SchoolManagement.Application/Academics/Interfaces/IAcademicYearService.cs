
using SchoolManagement.Application.Academics.DTOs;

namespace SchoolManagement.Application.Academics.Interfaces;

public interface IAcademicYearService
{
    Task<IReadOnlyList<AcademicYearDto>> GetAllAsync(
        string? search = null);

    Task<AcademicYearDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(SaveAcademicYearRequest request);

    Task UpdateAsync(int id, SaveAcademicYearRequest request);

    Task SetActiveAsync(int id);
}
