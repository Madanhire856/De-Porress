using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class StudentGuardian
{
    public Guid Id { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? GuardianId { get; set; }

    public string? Relationship { get; set; }

    public virtual Guardian? Guardian { get; set; }

    public virtual Student? Student { get; set; }
}
