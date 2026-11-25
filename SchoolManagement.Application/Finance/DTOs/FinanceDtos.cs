
namespace SchoolManagement.Application.Finance.DTOs;

public sealed record FinanceOptionDto(
    int Id,
    string Name
);

public sealed record FeeCategoryDto(
    int FeeCategoryId,
    string CategoryName,
    string? Description,
    bool IsActive
);

public sealed record SaveFeeCategoryRequest(
    string CategoryName,
    string? Description
);

public sealed record CreateStudentFeeRequest(
    int EnrollmentId,
    int FeeCategoryId,
    string? Description,
    decimal Amount,
    string CurrencyCode,
    DateOnly DueDate
);

public sealed record StudentFeeDto(
    int FeeId,
    int EnrollmentId,
    string StudentName,
    string StudentCode,
    string CategoryName,
    string? Description,
    decimal Amount,
    string CurrencyCode,
    DateOnly DueDate,
    decimal PaidAmount,
    decimal BalanceAmount,
    string PaymentStatus
);

public sealed record RecordFeePaymentRequest(
    int FeeId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNo,
    string? Note
);

public sealed record FeePaymentDto(
    int PaymentId,
    int FeeId,
    string StudentName,
    decimal Amount,
    string CurrencyCode,
    DateTime PaymentDate,
    string PaymentMethod,
    string? ReferenceNo,
    string? Note,
    int ReceivedBy
);

public sealed record FinanceTotalsDto(
    string CurrencyCode,
    decimal Assigned,
    decimal Paid,
    decimal Outstanding
);
