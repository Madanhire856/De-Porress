using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Grade
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int GradeLevel { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual User Creator { get; set; } = null!;
}
