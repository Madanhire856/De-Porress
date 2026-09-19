using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Class
{
    public Guid Id { get; set; }

    public Guid GradeId { get; set; }

    public string Name { get; set; } = null!;

    public int Capacity { get; set; }

    public Guid ClassTeacherId { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual Staff ClassTeacher { get; set; } = null!;

    public virtual User Creator { get; set; } = null!;

    public virtual Grade Grade { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
