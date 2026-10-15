using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Web.Services;

public interface IRbzRateService
{
    /// <summary>
    /// Returns the rate for 1 unit of `foreignCurrency` expressed in the system's
    /// base currency, for the given date. Checks the local cache first; only calls
    /// RBZ if the date isn't cached.
    /// </summary>
    Task<RbzRateResult?> GetRateAsync(
        string foreignCurrency,
        DateTime date,
        CancellationToken ct = default);
}

public class RbzRateResult
{
    /// <summary>1 unit of foreignCurrency = this many base units.</summary>
    public decimal Rate { get; set; }

    /// <summary>The date RBZ actually published the rate for.</summary>
    public DateTime ActualRateDate { get; set; }

    /// <summary>The date the caller asked for.</summary>
    public DateTime RequestedDate { get; set; }

    /// <summary>"RBZ" or "Manual" or "BASE".</summary>
    public string Source { get; set; } = "RBZ";

    /// <summary>True if we called the API this time (as opposed to a cache hit).</summary>
    public bool WasFetchedLive { get; set; }

    /// <summary>True if ActualRateDate != RequestedDate (weekend / holiday).</summary>
    public bool IsStale { get; set; }
}