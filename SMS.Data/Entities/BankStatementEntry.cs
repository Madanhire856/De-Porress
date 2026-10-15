using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class BankStatementEntry
{
    public Guid Id { get; set; }

    public DateTime? EntryDate { get; set; }

    public decimal? Amount { get; set; }

    public string? Description { get; set; }

    public string? BankReference { get; set; }

    public string CurrencyId { get; set; } = null!;

    public bool? IsMatched { get; set; }

    public Guid? MatchedPaymentId { get; set; }

    public DateTime? MatchedOn { get; set; }

    public Guid? MatchedByUserId { get; set; }

    public string? NotesJson { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Currency Currency { get; set; } = null!;

    public virtual User? MatchedByUser { get; set; }

    public virtual Payment? MatchedPayment { get; set; }
}
