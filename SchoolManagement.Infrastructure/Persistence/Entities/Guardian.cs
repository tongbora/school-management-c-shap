using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Guardian
{
    public int GuardianId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Occupation { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
}
