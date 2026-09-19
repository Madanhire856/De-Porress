using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Project
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int TypeId { get; set; }

    public Guid? ResponsiblePersonId { get; set; }

    public decimal? BudgetAmount { get; set; }

    public int StatusId { get; set; }

    public bool? IsPublic { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public virtual Staff? Creator { get; set; }

    public virtual User? CreatorNavigation { get; set; }

    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    public virtual ICollection<ProjectRisk> ProjectRisks { get; set; } = new List<ProjectRisk>();
}
