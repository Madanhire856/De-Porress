using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class ProjectMilestone
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime TargetDate { get; set; }

    public int PercentageComplete { get; set; }

    public string? PhotoUrl { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
