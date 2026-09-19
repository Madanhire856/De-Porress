using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class StudentLedger
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid TermId { get; set; }

    public int OpeningBalance { get; set; }

    public int ClosingBalance { get; set; }

    public string? CurrencyId { get; set; }

    public DateTime? CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public int? StatusId { get; set; }

    public virtual User? Creator { get; set; }

    public virtual Currency? Currency { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Student Student { get; set; } = null!;

    public virtual Term Term { get; set; } = null!;
}
