
using SchoolManagement.Application.Finance.DTOs;

namespace SchoolManagement.Application.Finance.Interfaces;

public interface IFinanceService
{
    Task<IReadOnlyList<FeeCategoryDto>>
        GetCategoriesAsync();

    Task<int> SaveCategoryAsync(
        SaveFeeCategoryRequest request,
        int? categoryId = null);

    Task SetCategoryActiveAsync(
        int categoryId,
        bool active);

    Task<IReadOnlyList<FinanceOptionDto>>
        GetActiveEnrollmentsAsync();

    Task<IReadOnlyList<StudentFeeDto>>
        GetFeesAsync(string? search = null);

    Task<int> CreateFeeAsync(
        CreateStudentFeeRequest request,
        int? createdBy = null);

    Task<IReadOnlyList<FeePaymentDto>>
        GetPaymentsAsync(int? feeId = null);

    Task<int> RecordPaymentAsync(
        RecordFeePaymentRequest request,
        int receivedBy);

    Task<IReadOnlyList<FinanceTotalsDto>>
        GetTotalsAsync();
}
