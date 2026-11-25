
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record SaveClassSubjectRequest(
    int ClassId,
    int SubjectId,
    int? TeacherId
);
