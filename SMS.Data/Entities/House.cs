using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class House
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public Guid MasterId { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Staff Master { get; set; } = null!;
}
