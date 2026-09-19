using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Village
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Headman { get; set; } = null!;

    public string Chief { get; set; } = null!;

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
