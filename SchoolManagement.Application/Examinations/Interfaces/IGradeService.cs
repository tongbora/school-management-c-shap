
using SchoolManagement.Application.Examinations.DTOs;

namespace SchoolManagement.Application.Examinations.Interfaces;

public interface IGradeService
{
    Task<IReadOnlyList<GradeDto>> GetAllAsync(
        string? search = null);

    Task<int> SaveAsync(
        SaveGradeRequest request,
        int? recordedBy = null);

    Task<IReadOnlyList<GradeOptionDto>>
        GetOpenExamsAsync();

    Task<IReadOnlyList<GradeOptionDto>>
        GetClassSubjectsAsync(int examId);

    Task<IReadOnlyList<GradeOptionDto>>
        GetEnrollmentsAsync(int classSubjectId);

    Task<IReadOnlyList<BulkGradeRowDto>> GetBulkRowsAsync(
        int examId,
        int classSubjectId);

    Task<int> SaveBulkAsync(
        SaveBulkGradesRequest request,
        int? recordedBy = null);

}
