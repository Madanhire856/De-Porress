using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class FeesStructure
{
    public Guid Id { get; set; }

    public Guid GradeId { get; set; }

    public Guid TermId { get; set; }

    public int LevyTypeId { get; set; }

    public decimal Amount { get; set; }

    public string? CurrencyId { get; set; }

    public Guid? CreatorId { get; set; }

    public DateTime? CreationDate { get; set; }

    public virtual User? Creator { get; set; }

    public virtual Currency? Currency { get; set; }
}
