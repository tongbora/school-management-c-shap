
using SchoolManagement.Application.Students.DTOs;

namespace SchoolManagement.Application.Students.Interfaces;

public interface IStudentService
{
    Task<IReadOnlyList<StudentDto>> GetStudentsAsync(
        string? search = null);

    Task<StudentDto?> GetByIdAsync(int studentId);

    Task<int> CreateAsync(SaveStudentRequest request);

    Task UpdateAsync(
        int studentId,
        SaveStudentRequest request);

    Task DeactivateAsync(int studentId);
}
