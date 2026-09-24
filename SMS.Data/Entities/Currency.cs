using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Currency
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Symbol { get; set; } = null!;

    public DateTime? CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public decimal? ExchangeRateToBase { get; set; }

    public bool IsBase { get; set; }

    public virtual ICollection<BankStatementEntry> BankStatementEntries { get; set; } = new List<BankStatementEntry>();

    public virtual User? Creator { get; set; }

    public virtual ICollection<FeesStructure> FeesStructures { get; set; } = new List<FeesStructure>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<SponsorshipAcquittal> SponsorshipAcquittals { get; set; } = new List<SponsorshipAcquittal>();

    public virtual ICollection<StudentLedger> StudentLedgers { get; set; } = new List<StudentLedger>();
}
