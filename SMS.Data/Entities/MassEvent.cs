using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class MassEvent
{
    public Guid Id { get; set; }

    public DateOnly LiturgicalDate { get; set; }

    public DateTime DateTime { get; set; }

    public string Venue { get; set; } = null!;

    public string Celebrant { get; set; } = null!;

    public Guid? CreatorId { get; set; }

    public DateTime? CreationDate { get; set; }

    public virtual User? Creator { get; set; }

    public virtual ICollection<MassRoster> MassRosters { get; set; } = new List<MassRoster>();
}
