
using SchoolManagement.Application.Academics.DTOs;

namespace SchoolManagement.Application.Academics.Interfaces;

public interface ISubjectService
{
    Task<IReadOnlyList<SubjectDto>> GetAllAsync(
        string? search = null);

    Task<SubjectDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(SaveSubjectRequest request);

    Task UpdateAsync(int id, SaveSubjectRequest request);

    Task SetActiveAsync(int id, bool isActive);
}
