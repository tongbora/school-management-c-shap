using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Fee
{
    public int FeeId { get; set; }

    public int EnrollmentId { get; set; }

    public int FeeCategoryId { get; set; }

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DateOnly DueDate { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual FeeCategory FeeCategory { get; set; } = null!;

    public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
}
