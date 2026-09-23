using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Term
{
    public Guid Id { get; set; }

    public int AcademicYear { get; set; }

    public int Number { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<StudentLedger> StudentLedgers { get; set; } = new List<StudentLedger>();
}
