using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid? LedgerId { get; set; }

    public int? Amount { get; set; }

    public string? CurrencyId { get; set; }

    public string? PaymentMethodId { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? ProofOfPaymentUrl { get; set; }

    public Guid? CreatorId { get; set; }

    public DateTime? CreationDate { get; set; }

    public virtual Currency? Currency { get; set; }

    public virtual User IdNavigation { get; set; } = null!;

    public virtual StudentLedger? Ledger { get; set; }
}
