using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class VwFeeBalance
{
    public int FeeId { get; set; }

    public int EnrollmentId { get; set; }

    public int FeeCategoryId { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DateOnly DueDate { get; set; }

    public decimal? PaidAmount { get; set; }

    public decimal? BalanceAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;
}
