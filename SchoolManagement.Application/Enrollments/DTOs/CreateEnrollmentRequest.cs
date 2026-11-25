
namespace SchoolManagement.Application.Enrollments.DTOs;

public sealed record CreateEnrollmentRequest(
    int StudentId,
    int ClassId,
    DateOnly EnrollmentDate
);
