using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid? LedgerId { get; set; }

    public decimal? Amount { get; set; }

    public string? CurrencyId { get; set; }

    public int PaymentMethodId { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? ProofOfPaymentUrl { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public decimal? BaseAmount { get; set; }

    public string CreatedByName { get; set; } = null!;

    public decimal? ExchangeRate { get; set; }

    public bool IsReversal { get; set; }

    public string? RateSource { get; set; }

    public int ReceiptNumber { get; set; }

    public int ReceiptYear { get; set; }

    public string? ReversalReason { get; set; }

    public Guid? ReversedById { get; set; }

    public DateTime? ReversedOn { get; set; }

    public Guid? ReversesPaymentId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Currency? Currency { get; set; }

    public virtual ICollection<Payment> InverseReversesPayment { get; set; } = new List<Payment>();

    public virtual StudentLedger? Ledger { get; set; }

    public virtual User? ReversedBy { get; set; }

    public virtual Payment? ReversesPayment { get; set; }
}
