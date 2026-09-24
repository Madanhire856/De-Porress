using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class DailyExchangeRate
{
    public Guid Id { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public DateTime RequestedDate { get; set; }

    public DateTime ActualRateDate { get; set; }

    public decimal Rate { get; set; }

    public string Source { get; set; } = null!;

    public string? RateType { get; set; }

    public DateTime FetchedOn { get; set; }
}
