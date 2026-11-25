
namespace SchoolManagement.Application.Dashboard.DTOs;

public sealed record DashboardSummary(
    int TotalStudents,
    int TotalTeachers,
    int TotalClasses,
    int TotalEnrollments
);
