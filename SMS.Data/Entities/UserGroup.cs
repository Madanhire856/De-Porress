using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class UserGroup
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public long? RightsId { get; set; }
    // Null on pre-existing rows preserves legacy access until the group is saved.
    // New groups start with no additional permissions.
    public long? FeatureRightsId { get; set; } = 0;
    public bool CanNominatePrefects { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
