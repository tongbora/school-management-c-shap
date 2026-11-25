using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string PermissionKey { get; set; } = null!;

    public string PermissionName { get; set; } = null!;

    public string ModuleName { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
