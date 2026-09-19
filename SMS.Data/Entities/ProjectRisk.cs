using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class ProjectRisk
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string DescriptionJson { get; set; } = null!;

    public int EscalationLevelId { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
