using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual User? User { get; set; }
}
