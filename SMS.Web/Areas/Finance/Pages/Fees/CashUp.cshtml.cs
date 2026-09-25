using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class CashUpModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CashUpModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Bursar { get; set; }

        /// <summary>
        /// "payment" (default) — reconcile against when money moved.
        /// "created" — audit view of what the bursar typed in today.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Basis { get; set; } = "payment";

        /// <summary>
        /// True if the current user may see every bursar's cash-up.
        /// When false, the page is silently scoped to the signed-in
        /// user's own payments and the Bursar filter is not honoured.
        /// </summary>
        public bool CanViewAllBursars { get; set; }

        // Header
        public DateTime SelectedDate { get; set; }
        public string SelectedDateFormatted { get; set; } = "";
        public string BasisLabel => Basis == "created"
            ? "Entries made on this date"
            : "Money received on this date";
        public string ReportTitle => Basis == "created"
            ? "Daily Entry Report"
            : "Daily Cash-Up Report";
        public string BaseCurrency { get; set; } = "USD";
        public string GeneratedByName { get; set; } = "";
        public DateTime GeneratedOn { get; set; }

        // Data
        public List<BursarOption> BursarOptions { get; set; } = new();
        public List<CurrencyGroup> CurrencyGroups { get; set; } = new();
        public List<MethodGroup> MethodGroups { get; set; } = new();
        public List<BursarGroup> BursarGroups { get; set; } = new();
        public List<PaymentLine> Payments { get; set; } = new();
        public List<PaymentLine> Reversals { get; set; } = new();

        // Totals
        public int TotalPaymentsCount { get; set; }
        public int TotalReversalsCount { get; set; }
        public int LateEntryCount { get; set; }
        public int ManualRateCount { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalReversed { get; set; }
        public decimal NetCollected { get; set; }

        public int? FirstReceiptNumber { get; set; }
        public int? LastReceiptNumber { get; set; }
        public int CurrentReceiptYear { get; set; }

        // =============================================================
        //  DTOs
        // =============================================================
        public class BursarOption
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
        }

        public class CurrencyGroup
        {
            public string CurrencyCode { get; set; } = "";
            public int Count { get; set; }
            public decimal TotalAmount { get; set; }      // in that currency
            public decimal TotalBaseAmount { get; set; }  // in base currency
        }

        public class MethodGroup
        {
            public string MethodName { get; set; } = "";
            public string CurrencyCode { get; set; } = "";
            public int Count { get; set; }
            public decimal Total { get; set; }        // in original currency
            public decimal BaseTotal { get; set; }    // in base currency
        }

        public class BursarGroup
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
            public decimal BaseTotal { get; set; }
        }

        public class PaymentLine
        {
            public Guid Id { get; set; }
            public int ReceiptNumber { get; set; }
            public int ReceiptYear { get; set; }
            public string ReceiptDisplay => $"RCP-{ReceiptYear}-{ReceiptNumber:D5}";

            // Two dates — money date and entry date
            public DateTime PaymentDate { get; set; }
            public DateTime CreationDate { get; set; }
            public bool IsBackdated => PaymentDate.Date != CreationDate.Date;

            public string StudentName { get; set; } = "";
            public string Method { get; set; } = "";

            // Amount in original currency
            public decimal Amount { get; set; }
            public string CurrencyId { get; set; } = "";

            // Rate used
            public decimal ExchangeRate { get; set; }
            public string? RateSource { get; set; }
            public bool IsManualRate =>
                string.Equals(RateSource, "Manual", StringComparison.OrdinalIgnoreCase);
            public bool IsBaseCurrencyRate =>
                string.Equals(RateSource, "BASE", StringComparison.OrdinalIgnoreCase);

            // Base amount (in school currency)
            public decimal BaseAmount { get; set; }

            public string? Reference { get; set; }
            public string? ReversalReason { get; set; }
            public string CreatedByName { get; set; } = "";
            public bool IsReversal { get; set; }
        }

        // =============================================================
        //  GET
        // =============================================================
        public IActionResult OnGet()
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            // Normalise basis
            if (Basis != "created") Basis = "payment";

            // ---- Scope check ----
            // The right controls whether the user can see every bursar's
            // takings, or only their own. Without it, any bursar filter
            // they try to pass in the query string is discarded and the
            // query is constrained to payments they personally created.
            CanViewAllBursars = _currentUser.HasRight(AccessRights.ViewAllBursarsCashUp);

            if (!CanViewAllBursars)
                Bursar = null;

            SelectedDate = (Date ?? DateTime.Today).Date;
            SelectedDateFormatted = SelectedDate.ToString("dddd, dd MMMM yyyy");

            BaseCurrency = _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefault() ?? "USD";

            var dayStart = SelectedDate;
            var dayEnd = dayStart.AddDays(1);

            // ---- Fetch raw payments for the day, by chosen basis ----
            var query = Basis == "created"
                ? _context.Payments.AsNoTracking()
                    .Where(p => p.CreationDate >= dayStart && p.CreationDate < dayEnd)
                : _context.Payments.AsNoTracking()
                    .Where(p => p.PaymentDate >= dayStart && p.PaymentDate < dayEnd);

            // ---- Scope to the signed-in user when the right is absent ----
            if (!CanViewAllBursars)
            {
                var me = _currentUser.UserId ?? Guid.Empty;
                query = query.Where(p => p.CreatorId == me);
            }

            var rawPayments = query
                .OrderBy(p => p.ReceiptNumber)
                .ToList();

            // ---- Bursar filter options ----
            // Only meaningful when the user is allowed to see multiple
            // bursars. Scoped users see their own single name.
            BursarOptions = rawPayments
                .Where(p => !string.IsNullOrWhiteSpace(p.CreatedByName))
                .GroupBy(p => p.CreatedByName!)
                .Select(g => new BursarOption
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderBy(b => b.Name)
                .ToList();

            // ---- Apply bursar filter (no-op when scoped, since Bursar was nulled) ----
            var filteredPayments = string.IsNullOrWhiteSpace(Bursar)
                ? rawPayments
                : rawPayments.Where(p => p.CreatedByName == Bursar).ToList();

            // ---- Student names in one query ----
            var ledgerIds = filteredPayments
                .Where(p => p.LedgerId.HasValue)
                .Select(p => p.LedgerId!.Value)
                .Distinct()
                .ToList();

            var studentByLedger = _context.StudentLedgers
                .AsNoTracking()
                .Include(l => l.Student)
                .Where(l => ledgerIds.Contains(l.Id))
                .ToDictionary(
                    l => l.Id,
                    l => $"{l.Student.Name} {l.Student.Surname}".Trim());

            // ---- Build line items ----
            var lines = filteredPayments
                .Select(p =>
                {
                    var method = EnumExtensions.ParsePaymentMethod(p.PaymentMethodId);
                    var studentName = p.LedgerId.HasValue
                        && studentByLedger.TryGetValue(p.LedgerId.Value, out var n)
                        ? n
                        : "—";

                    return new PaymentLine
                    {
                        Id = p.Id,
                        ReceiptNumber = p.ReceiptNumber,
                        ReceiptYear = p.ReceiptYear,
                        PaymentDate = p.PaymentDate,
                        CreationDate = p.CreationDate,
                        StudentName = studentName,
                        Method = method.HasValue ? method.Value.ToDisplayName() : "—",
                        Amount = p.Amount ?? 0m,
                        CurrencyId = p.CurrencyId ?? BaseCurrency,
                        ExchangeRate = (decimal)p.ExchangeRate,
                        RateSource = p.RateSource,
                        BaseAmount = p.BaseAmount ?? p.Amount ?? 0m,
                        Reference = p.ReferenceNumber,
                        ReversalReason = p.ReversalReason,
                        CreatedByName = p.CreatedByName ?? "",
                        IsReversal = p.IsReversal
                    };
                })
                .ToList();

            Payments = lines.Where(l => !l.IsReversal)
                            .OrderBy(l => l.ReceiptNumber)
                            .ToList();

            Reversals = lines.Where(l => l.IsReversal)
                             .OrderBy(l => l.ReceiptNumber)
                             .ToList();

            // ---- Totals ----
            TotalPaymentsCount = Payments.Count;
            TotalReversalsCount = Reversals.Count;
            LateEntryCount = Payments.Count(l => l.IsBackdated);
            ManualRateCount = Payments.Count(l => l.IsManualRate);

            TotalCollected = Payments.Sum(l => l.BaseAmount);
            TotalReversed = Reversals.Sum(l => Math.Abs(l.BaseAmount));
            NetCollected = TotalCollected - TotalReversed;

            // ---- Receipt range (payments only) ----
            if (Payments.Any())
            {
                FirstReceiptNumber = Payments.First().ReceiptNumber;
                LastReceiptNumber = Payments.Last().ReceiptNumber;
                CurrentReceiptYear = Payments.First().ReceiptYear;
            }

            // ---- Per-currency totals ----
            CurrencyGroups = Payments
                .GroupBy(l => l.CurrencyId)
                .Select(g => new CurrencyGroup
                {
                    CurrencyCode = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    TotalBaseAmount = g.Sum(x => x.BaseAmount)
                })
                .OrderByDescending(c => c.TotalBaseAmount)
                .ToList();

            // ---- Per-method totals, split by currency ----
            MethodGroups = Payments
                .GroupBy(l => new { l.Method, l.CurrencyId })
                .Select(g => new MethodGroup
                {
                    MethodName = g.Key.Method,
                    CurrencyCode = g.Key.CurrencyId,
                    Count = g.Count(),
                    Total = g.Sum(x => x.Amount),
                    BaseTotal = g.Sum(x => x.BaseAmount)
                })
                .OrderByDescending(g => g.BaseTotal)
                .ThenBy(g => g.MethodName)
                .ToList();

            BursarGroups = Payments
                .Where(l => !string.IsNullOrWhiteSpace(l.CreatedByName))
                .GroupBy(l => l.CreatedByName)
                .Select(g => new BursarGroup
                {
                    Name = g.Key,
                    Count = g.Count(),
                    BaseTotal = g.Sum(x => x.BaseAmount)
                })
                .OrderByDescending(g => g.BaseTotal)
                .ToList();

            GeneratedByName = User.Identity?.Name ?? "System";
            GeneratedOn = DateTime.Now;

            return Page();
        }
    }
}