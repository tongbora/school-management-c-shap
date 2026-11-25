
using SchoolManagement.Application.Teachers.DTOs;

namespace SchoolManagement.Application.Teachers.Interfaces;

public interface ITeacherService
{
    Task<IReadOnlyList<TeacherDto>> GetTeachersAsync(
        string? search = null);

    Task<TeacherDto?> GetByIdAsync(int teacherId);

    Task<IReadOnlyList<DepartmentOptionDto>>
        GetDepartmentsAsync();

    Task<int> CreateAsync(SaveTeacherRequest request);

    Task UpdateAsync(
        int teacherId,
        SaveTeacherRequest request);

    Task DeactivateAsync(int teacherId);
}
