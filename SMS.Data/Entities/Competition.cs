using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Competition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Tier { get; set; } = null!;

    public int AcademicYear { get; set; }

    public Guid TermId { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Term Term { get; set; } = null!;
}
