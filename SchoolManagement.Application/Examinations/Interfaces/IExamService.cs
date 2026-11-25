
using SchoolManagement.Application.Examinations.DTOs;

namespace SchoolManagement.Application.Examinations.Interfaces;

public interface IExamService
{
    Task<IReadOnlyList<ExamDto>> GetAllAsync(
        string? search = null);

    Task<ExamDto?> GetByIdAsync(int examId);

    Task<int> CreateAsync(SaveExamRequest request);

    Task UpdateAsync(int examId, SaveExamRequest request);
    Task SetStatusAsync(int examId, string status);

}
