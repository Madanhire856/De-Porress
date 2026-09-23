using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Sport
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<SportTeacher> SportTeachers { get; set; } = new List<SportTeacher>();
}
