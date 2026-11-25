
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using SchoolManagement.Application.Finance.DTOs;
using SchoolManagement.Application.Finance.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public FinanceService(
        IDbContextFactory<SchoolDbContext> factory)
    {
        _factory = factory;
    }

    // =========================================
    // Fee Categories
    // =========================================

    public async Task<IReadOnlyList<FeeCategoryDto>>
        GetCategoriesAsync()
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        return await db.FeeCategories
            .AsNoTracking()
            .OrderBy(x => x.CategoryName)
            .Select(x => new FeeCategoryDto(
                x.FeeCategoryId,
                x.CategoryName,
                x.Description,
                x.IsActive))
            .ToListAsync();
    }

    public async Task<int> SaveCategoryAsync(
        SaveFeeCategoryRequest request,
        int? categoryId = null)
    {
        string name = request.CategoryName?.Trim() ?? "";

        if (name.Length < 1 || name.Length > 100)
            throw new ArgumentException(
                "Category name must contain 1-100 characters.");

        string? description = Clean(request.Description);

        if (description?.Length > 500)
            throw new ArgumentException(
                "Category description exceeds 500 characters.");

        await using var db =
            await _factory.CreateDbContextAsync();

        bool duplicate = await db.FeeCategories.AnyAsync(x =>
            x.CategoryName == name &&
            x.FeeCategoryId != (categoryId ?? 0));

        if (duplicate)
            throw new InvalidOperationException(
                "Category name already exists.");

        FeeCategory category;

        if (categoryId.HasValue)
        {
            category = await db.FeeCategories
                .FindAsync(categoryId.Value)
                ?? throw new InvalidOperationException(
                    "Fee category not found.");

            category.CategoryName = name;
            category.Description = description;
        }
        else
        {
            category = new FeeCategory
            {
                CategoryName = name,
                Description = description,
                IsActive = true
            };

            db.FeeCategories.Add(category);
        }

        await db.SaveChangesAsync();

        return category.FeeCategoryId;
    }

    public async Task SetCategoryActiveAsync(
        int categoryId,
        bool active)
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        var category = await db.FeeCategories
            .FindAsync(categoryId)
            ?? throw new InvalidOperationException(
                "Fee category not found.");

        category.IsActive = active;

        // Existing fees remain unchanged.
        await db.SaveChangesAsync();
    }

    // =========================================
    // Enrollments for fee assignment
    // =========================================

    public async Task<IReadOnlyList<FinanceOptionDto>>
        GetActiveEnrollmentsAsync()
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        return await db.Enrollments
            .AsNoTracking()
            .Where(x =>
                x.Status == "Active" &&
                x.Student.Status == "Active")
            .OrderBy(x => x.Student.FullName)
            .Select(x => new FinanceOptionDto(
                x.EnrollmentId,
                x.Student.FullName +
                " (" + x.Student.StudentCode + ")" +
                " - " + x.Class.ClassName))
            .ToListAsync();
    }

    // =========================================
    // Create a student fee
    // =========================================

    public async Task<int> CreateFeeAsync(
        CreateStudentFeeRequest request,
        int? createdBy = null)
    {
        if (request.EnrollmentId <= 0 ||
            request.FeeCategoryId <= 0)
        {
            throw new ArgumentException(
                "Select an enrollment and fee category.");
        }

        ValidateMoney(request.Amount);

        if (request.Amount == 0)
            throw new ArgumentException(
                "Fee amount must be greater than zero.");

        string currency = (request.CurrencyCode ?? "")
            .Trim()
            .ToUpperInvariant();

        if (currency != "USD" && currency != "KHR")
            throw new ArgumentException(
                "Choose USD or KHR.");

        string? description = Clean(request.Description);

        if (description?.Length > 255)
            throw new ArgumentException(
                "Fee description exceeds 255 characters.");

        await using var db =
            await _factory.CreateDbContextAsync();

        var enrollment = await db.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.EnrollmentId == request.EnrollmentId);

        if (enrollment is null ||
            enrollment.Status != "Active")
        {
            throw new InvalidOperationException(
                "Select an active enrollment.");
        }

        bool categoryValid = await db.FeeCategories
            .AnyAsync(x =>
                x.FeeCategoryId == request.FeeCategoryId &&
                x.IsActive);

        if (!categoryValid)
            throw new InvalidOperationException(
                "Select an active fee category.");

        if (createdBy.HasValue)
        {
            bool validUser = await db.Users.AnyAsync(x =>
                x.UserId == createdBy.Value &&
                x.IsActive);

            if (!validUser)
                throw new InvalidOperationException(
                    "Invalid creating user.");
        }

        var fee = new Fee
        {
            EnrollmentId = request.EnrollmentId,
            FeeCategoryId = request.FeeCategoryId,
            Description = description,
            Amount = request.Amount,
            CurrencyCode = currency,
            DueDate = request.DueDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        db.Fees.Add(fee);

        await db.SaveChangesAsync();

        return fee.FeeId;
    }

    // =========================================
    // Get fees and balances
    // =========================================

    public async Task<IReadOnlyList<StudentFeeDto>>
        GetFeesAsync(string? search = null)
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        // Use the existing SQL Server view.
        var balances = await db.Database
            .SqlQueryRaw<BalanceViewRow>(
                """
                SELECT
                    FeeId,
                    EnrollmentId,
                    FeeCategoryId,
                    Amount,
                    CurrencyCode,
                    DueDate,
                    PaidAmount,
                    BalanceAmount,
                    PaymentStatus
                FROM dbo.vwFeeBalances
                """)
            .ToListAsync();

        var metadata = await db.Fees
            .AsNoTracking()
            .Select(f => new
            {
                f.FeeId,
                f.Description,
                f.Enrollment.Student.FullName,
                f.Enrollment.Student.StudentCode,
                f.FeeCategory.CategoryName
            })
            .ToListAsync();

        var lookup = metadata.ToDictionary(
            x => x.FeeId);

        var result = new List<StudentFeeDto>();

        foreach (var balance in balances)
        {
            if (!lookup.TryGetValue(
                balance.FeeId, out var item))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                bool matched =
                    item.FullName.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    item.StudentCode.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    item.CategoryName.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    balance.PaymentStatus.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase);

                if (!matched)
                    continue;
            }

            result.Add(new StudentFeeDto(
                balance.FeeId,
                balance.EnrollmentId,
                item.FullName,
                item.StudentCode,
                item.CategoryName,
                item.Description,
                balance.Amount,
                balance.CurrencyCode.Trim(),
                balance.DueDate,
                balance.PaidAmount ?? 0m,
                balance.BalanceAmount ?? balance.Amount,
                balance.PaymentStatus));
        }

        return result
            .OrderByDescending(x => x.DueDate)
            .ThenByDescending(x => x.FeeId)
            .ToList();
    }

    // =========================================
    // Payment history
    // =========================================

    public async Task<IReadOnlyList<FeePaymentDto>>
        GetPaymentsAsync(int? feeId = null)
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        var query = db.FeePayments.AsNoTracking();

        if (feeId.HasValue)
        {
            query = query.Where(x =>
                x.FeeId == feeId.Value);
        }

        return await query
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.PaymentId)
            .Select(x => new FeePaymentDto(
                x.PaymentId,
                x.FeeId,
                x.Fee.Enrollment.Student.FullName,
                x.Amount,
                x.Fee.CurrencyCode,
                x.PaymentDate,
                x.PaymentMethod,
                x.ReferenceNo,
                x.Note,
                x.ReceivedBy))
            .Take(500)
            .ToListAsync();
    }

    // =========================================
    // Record a payment
    // =========================================

    public async Task<int> RecordPaymentAsync(
        RecordFeePaymentRequest request,
        int receivedBy)
    {
        if (request.FeeId <= 0 || receivedBy <= 0)
            throw new ArgumentException(
                "A fee and authenticated receiving user are required.");

        ValidateMoney(request.Amount);

        if (request.Amount == 0)
            throw new ArgumentException(
                "Payment must be greater than zero.");

        string method =
            (request.PaymentMethod ?? "").Trim();

        if (method.Length < 1 || method.Length > 30)
            throw new ArgumentException(
                "Payment method must contain 1-30 characters.");

        string? reference = Clean(request.ReferenceNo);
        string? note = Clean(request.Note);

        if (reference?.Length > 100)
            throw new ArgumentException(
                "Payment reference exceeds 100 characters.");

        if (note?.Length > 500)
            throw new ArgumentException(
                "Payment note exceeds 500 characters.");

        await using var root =
            await _factory.CreateDbContextAsync();

        var strategy =
            root.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var db =
                await _factory.CreateDbContextAsync();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            bool validUser = await db.Users
                .AnyAsync(x =>
                    x.UserId == receivedBy &&
                    x.IsActive);

            if (!validUser)
                throw new InvalidOperationException(
                    "Authenticated user is invalid or inactive.");

            // Lock the fee row while checking balance.
            var fee = await db.Fees
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM dbo.Fees WITH (UPDLOCK, HOLDLOCK)
                    WHERE FeeId = {request.FeeId}
                    """)
                .SingleOrDefaultAsync()
                ?? throw new InvalidOperationException(
                    "Fee not found.");

            if (reference is not null)
            {
                bool referenceExists =
                    await db.FeePayments.AnyAsync(x =>
                        x.ReferenceNo == reference);

                if (referenceExists)
                    throw new InvalidOperationException(
                        "Payment reference already exists.");
            }

            decimal paid = await db.FeePayments
                .Where(x => x.FeeId == fee.FeeId)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            decimal remaining = fee.Amount - paid;

            if (remaining <= 0)
                throw new InvalidOperationException(
                    "This fee has already been paid.");

            if (request.Amount > remaining)
                throw new InvalidOperationException(
                    $"Payment exceeds outstanding balance " +
                    $"({remaining:0.00} " +
                    $"{fee.CurrencyCode.Trim()}).");

            var now = DateTime.UtcNow;

            var payment = new FeePayment
            {
                FeeId = fee.FeeId,
                Amount = request.Amount,
                PaymentDate = now,
                PaymentMethod = method,
                ReferenceNo = reference,
                Note = note,
                ReceivedBy = receivedBy,
                CreatedAt = now
            };

            db.FeePayments.Add(payment);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return payment.PaymentId;
        });
    }

    // =========================================
    // Finance summary by currency
    // =========================================

    public async Task<IReadOnlyList<FinanceTotalsDto>>
        GetTotalsAsync()
    {
        var fees = await GetFeesAsync();

        return fees
            .GroupBy(x => x.CurrencyCode)
            .OrderBy(x => x.Key)
            .Select(group => new FinanceTotalsDto(
                group.Key,
                group.Sum(x => x.Amount),
                group.Sum(x => x.PaidAmount),
                group.Sum(x => x.BalanceAmount)))
            .ToList();
    }

    // =========================================
    // Validation helpers
    // =========================================

    private static void ValidateMoney(decimal value)
    {
        if (value < 0 ||
            decimal.Round(value, 2) != value ||
            value > 9999999999999999.99m)
        {
            throw new ArgumentException(
                "Amount must be nonnegative, " +
                "use at most two decimal places, " +
                "and fit DECIMAL(18,2).");
        }
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // =========================================
    // SQL View result model
    // =========================================

    public sealed class BalanceViewRow
    {
        public int FeeId { get; set; }

        public int EnrollmentId { get; set; }

        public int FeeCategoryId { get; set; }

        public decimal Amount { get; set; }

        public string CurrencyCode { get; set; } = "";

        public DateOnly DueDate { get; set; }

        public decimal? PaidAmount { get; set; }

        public decimal? BalanceAmount { get; set; }

        public string PaymentStatus { get; set; } = "";
    }
}
