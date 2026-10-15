using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class PaymentCorrection
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Guid CorrectedById { get; set; }

    public string CorrectedByName { get; set; } = null!;

    public DateTime CorrectedOn { get; set; }

    public string Reason { get; set; } = null!;

    public decimal PreviousAmount { get; set; }

    public decimal CorrectedAmount { get; set; }

    public decimal PreviousBaseAmount { get; set; }

    public decimal CorrectedBaseAmount { get; set; }

    public DateTime PreviousPaymentDate { get; set; }

    public DateTime CorrectedPaymentDate { get; set; }

    public int PreviousPaymentMethodId { get; set; }

    public int CorrectedPaymentMethodId { get; set; }

    public string? PreviousReferenceNumber { get; set; }

    public string? CorrectedReferenceNumber { get; set; }

    public virtual User CorrectedBy { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;
}
