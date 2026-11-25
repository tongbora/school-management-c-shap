
using SchoolManagement.Application.Enrollments.DTOs;

namespace SchoolManagement.Application.Enrollments.Interfaces;

public interface IEnrollmentService
{
    Task<IReadOnlyList<EnrollmentDto>> GetAllAsync(
        string? search = null);

    Task<int> CreateAsync(CreateEnrollmentRequest request);

    Task<int> TransferAsync(TransferEnrollmentRequest request);

    Task SetStatusAsync(
        int enrollmentId,
        string status,
        DateOnly endDate);

    Task<IReadOnlyList<EnrollmentOptionDto>> GetStudentsAsync();

    Task<IReadOnlyList<EnrollmentOptionDto>> GetClassesAsync();
}
