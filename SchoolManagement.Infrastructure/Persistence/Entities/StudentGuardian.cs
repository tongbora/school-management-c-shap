using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class StudentGuardian
{
    public int StudentId { get; set; }

    public int GuardianId { get; set; }

    public string Relationship { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public virtual Guardian Guardian { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
