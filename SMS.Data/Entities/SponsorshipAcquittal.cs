using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class SponsorshipAcquittal
{
    public Guid Id { get; set; }

    public Guid SponsorshipId { get; set; }

    public string Period { get; set; } = null!;

    public decimal? AmountDisbursed { get; set; }

    public decimal? AmountUtilised { get; set; }

    public string? CurrencyId { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Currency? Currency { get; set; }

    public virtual Sponsorship Sponsorship { get; set; } = null!;
}
