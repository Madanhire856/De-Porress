using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Subject
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public int CategoryId { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
