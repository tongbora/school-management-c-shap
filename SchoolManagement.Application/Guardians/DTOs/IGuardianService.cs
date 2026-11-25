
using SchoolManagement.Application.Guardians.DTOs;

namespace SchoolManagement.Application.Guardians.Interfaces;

public interface IGuardianService
{
    Task<IReadOnlyList<GuardianDto>> SearchAsync(
        string? keyword = null);

    Task<IReadOnlyList<StudentGuardianDto>>
        GetStudentGuardiansAsync(int studentId);

    Task<int> CreateAsync(SaveGuardianRequest request);

    Task UpdateAsync(
        int guardianId,
        SaveGuardianRequest request);

    Task LinkAsync(
        int studentId,
        int guardianId,
        string relationship,
        bool isPrimary);

    Task UnlinkAsync(int studentId, int guardianId);

    Task SetPrimaryAsync(int studentId, int guardianId);
}
