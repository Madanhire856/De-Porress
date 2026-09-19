using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class TeacherSubject
{
    public Guid Id { get; set; }

    public Guid SubjectId { get; set; }

    public Guid StaffId { get; set; }

    public virtual Staff Staff { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
