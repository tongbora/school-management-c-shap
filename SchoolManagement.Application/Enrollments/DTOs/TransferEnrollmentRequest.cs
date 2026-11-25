
namespace SchoolManagement.Application.Enrollments.DTOs;

public sealed record TransferEnrollmentRequest(
    int EnrollmentId,
    int TargetClassId,
    DateOnly TransferDate
);
