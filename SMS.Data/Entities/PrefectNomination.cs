using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class PrefectNomination
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public int? PostTitleId { get; set; }

    public int StatusId { get; set; }

    public string? Reason { get; set; }

    public Guid NominatedByUserId { get; set; }

    public DateTime CreationDate { get; set; }

    public virtual User NominatedByUser { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
