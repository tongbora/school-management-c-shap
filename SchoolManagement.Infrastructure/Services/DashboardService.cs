
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Dashboard.DTOs;
using SchoolManagement.Application.Dashboard.Interfaces;
using SchoolManagement.Infrastructure.Persistence;

namespace SchoolManagement.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<SchoolDbContext> _dbFactory;

    public DashboardService(
        IDbContextFactory<SchoolDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync(
                cancellationToken);

        int students = await db.Students
            .AsNoTracking()
            .CountAsync(cancellationToken);

        int teachers = await db.Teachers
            .AsNoTracking()
            .CountAsync(cancellationToken);

        int classes = await db.Classes
            .AsNoTracking()
            .CountAsync(cancellationToken);

        int enrollments = await db.Enrollments
            .AsNoTracking()
            .CountAsync(cancellationToken);

        return new DashboardSummary(
            students,
            teachers,
            classes,
            enrollments
        );
    }
}
