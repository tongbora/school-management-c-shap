using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class FeePayment
{
    public int PaymentId { get; set; }

    public int FeeId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public int ReceivedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Fee Fee { get; set; } = null!;

    public virtual User ReceivedByNavigation { get; set; } = null!;
}
