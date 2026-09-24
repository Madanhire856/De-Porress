using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Data;

namespace SMS.Web.Services;

public class RbzRateService : IRbzRateService
{
    private readonly SMSDbContext _context;
    private readonly HttpClient _http;
    private readonly RbzApiOptions _options;
    private readonly ILogger<RbzRateService> _logger;

    public RbzRateService(
        SMSDbContext context,
        HttpClient http,
        IOptions<RbzApiOptions> options,
        ILogger<RbzRateService> logger)
    {
        _context = context;
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RbzRateResult?> GetRateAsync(
        string foreignCurrency,
        DateTime date,
        CancellationToken ct = default)
    {
        // ---- Resolve base currency from the DB ----
        var baseCurrency = await _context.Currencies
            .AsNoTracking()
            .Where(c => c.IsBase == true)
            .Select(c => c.Code)
            .FirstOrDefaultAsync(ct) ?? "USD";

        // ---- Same currency → rate is 1, no API call ----
        if (string.Equals(foreignCurrency, baseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return new RbzRateResult
            {
                Rate = 1m,
                ActualRateDate = date.Date,
                RequestedDate = date.Date,
                Source = "BASE",
                WasFetchedLive = false,
                IsStale = false
            };
        }

        var requested = date.Date;

        // ---- Cache check ----
        var cached = await _context.DailyExchangeRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.CurrencyCode == foreignCurrency && r.RequestedDate == requested, ct);

        if (cached != null)
        {
            return new RbzRateResult
            {
                Rate = cached.Rate,
                ActualRateDate = cached.ActualRateDate,
                RequestedDate = requested,
                Source = cached.Source,
                WasFetchedLive = false,
                IsStale = cached.ActualRateDate != requested
            };
        }

        // ---- Fetch from RBZ ----
        var url = $"{_options.BaseUrl.TrimEnd('/')}/latest" +
                  $"?source={baseCurrency}&target={foreignCurrency}";

        _logger.LogInformation("RBZ fetch: {Url}", url);

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RBZ API request failed for {Currency} on {Date}", foreignCurrency, requested);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("RBZ API returned {Status} for {Currency} on {Date}",
                (int)response.StatusCode, foreignCurrency, requested);
            return null;
        }

        RbzApiResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<RbzApiResponse>(
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse RBZ response");
            return null;
        }

        if (payload == null || payload.Rate <= 0m)
        {
            _logger.LogWarning("RBZ returned invalid rate for {Currency}", foreignCurrency);
            return null;
        }

        // ---- API returns "1 base = payload.rate foreign". We want "1 foreign = X base". ----
        // Round to 6 decimals — matches the DB column precision (decimal(18,6)) and
        // prevents the 28-digit floating-point noise that decimal division produces.
        var basePerForeign = Math.Round(
            1m / payload.Rate,
            6,
            MidpointRounding.AwayFromZero);

        var actualDate = payload.RateDate.Date;

        // ---- Cache ----
        var row = new DailyExchangeRate
        {
            Id = Guid.NewGuid(),
            CurrencyCode = foreignCurrency,
            RequestedDate = requested,
            ActualRateDate = actualDate,
            Rate = basePerForeign,
            Source = "RBZ",
            RateType = payload.RateType,
            FetchedOn = DateTime.Now
        };

        try
        {
            _context.DailyExchangeRates.Add(row);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Cache write failed (unique constraint race, etc.) — log but return the rate anyway.
            _logger.LogWarning(ex,
                "Failed to cache RBZ rate for {Currency} on {Date}", foreignCurrency, requested);
        }

        return new RbzRateResult
        {
            Rate = basePerForeign,
            ActualRateDate = actualDate,
            RequestedDate = requested,
            Source = "RBZ",
            WasFetchedLive = true,
            IsStale = actualDate != requested
        };
    }

    // ---- Raw response shape from AllRatesToday ----
    private class RbzApiResponse
    {
        [JsonPropertyName("bank")] public string? Bank { get; set; }
        [JsonPropertyName("rate_date")] public DateTime RateDate { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("target")] public string? Target { get; set; }
        [JsonPropertyName("rate")] public decimal Rate { get; set; }
        [JsonPropertyName("rate_type")] public string? RateType { get; set; }
    }
}