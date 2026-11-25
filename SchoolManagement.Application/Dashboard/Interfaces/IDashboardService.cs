
using SchoolManagement.Application.Dashboard.DTOs;

namespace SchoolManagement.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}
