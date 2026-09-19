using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Sponsorship
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public int? SponsorTypeId { get; set; }

    public string SponsorName { get; set; } = null!;

    public int PercentageCoverage { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public Guid? CreatorId { get; set; }

    public DateTime? CreationDate { get; set; }

    public virtual User? Creator { get; set; }

    public virtual ICollection<SponsorshipAcquittal> SponsorshipAcquittals { get; set; } = new List<SponsorshipAcquittal>();
}
