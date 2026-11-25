
using SchoolManagement.Application.Academics.DTOs;

namespace SchoolManagement.Application.Academics.Interfaces;

public interface IClassSubjectService
{
    Task<IReadOnlyList<ClassSubjectDto>> GetAllAsync(
        string? search = null);

    Task<ClassSubjectDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(SaveClassSubjectRequest request);

    Task UpdateAsync(int id, SaveClassSubjectRequest request);

    Task DeleteAsync(int id);

    Task<IReadOnlyList<AcademicOptionDto>> GetClassesAsync();

    Task<IReadOnlyList<AcademicOptionDto>> GetSubjectsAsync();

    Task<IReadOnlyList<AcademicOptionDto>> GetTeachersAsync();
}
