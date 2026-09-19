using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class MassRoster
{
    public Guid Id { get; set; }

    public Guid MassId { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? StaffId { get; set; }

    public int RoleId { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public virtual MassEvent Mass { get; set; } = null!;

    public virtual Staff? Staff { get; set; }

    public virtual Student? Student { get; set; }
}
