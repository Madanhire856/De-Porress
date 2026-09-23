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

        // Header
        public DateTime SelectedDate { get; set; }
        public string SelectedDateFormatted { get; set; } = "";
        public string BaseCurrency { get; set; } = "USD";
        public string GeneratedByName { get; set; } = "";
        public DateTime GeneratedOn { get; set; }

        // Data
        public List<BursarOption> BursarOptions { get; set; } = new();
        public List<MethodGroup> MethodGroups { get; set; } = new();
        public List<PaymentLine> Payments { get; set; } = new();
        public List<PaymentLine> Reversals { get; set; } = new();

        // Totals — now decimal
        public int TotalPaymentsCount { get; set; }
        public int TotalReversalsCount { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalReversed { get; set; }
        public decimal NetCollected { get; set; }

        public int? FirstReceiptNumber { get; set; }
        public int? LastReceiptNumber { get; set; }
        public int CurrentReceiptYear { get; set; }

        public class BursarOption
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
        }

        public class MethodGroup
        {
            public string MethodName { get; set; } = "";
            public int Count { get; set; }
            public decimal Total { get; set; }       // ← was long
        }

        public class PaymentLine
        {
            public Guid Id { get; set; }
            public int ReceiptNumber { get; set; }
            public int ReceiptYear { get; set; }
            public string ReceiptDisplay => $"RCP-{ReceiptYear}-{ReceiptNumber:D5}";
            public DateTime Date { get; set; }
            public string StudentName { get; set; } = "";
            public string Method { get; set; } = "";
            public decimal Amount { get; set; }      // ← was int
            public string CurrencyId { get; set; } = "";
            public decimal BaseAmount { get; set; }  // ← was int
            public string? Reference { get; set; }
            public string? ReversalReason { get; set; }
            public string CreatedByName { get; set; } = "";
            public bool IsReversal { get; set; }
        }

        public IActionResult OnGet()
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            SelectedDate = (Date ?? DateTime.Today).Date;
            SelectedDateFormatted = SelectedDate.ToString("dddd, dd MMMM yyyy");

            // Base currency — nullable IsBase, Code is PK
            BaseCurrency = _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefault() ?? "USD";

            var dayStart = SelectedDate;
            var dayEnd = dayStart.AddDays(1);

            // All payments (and reversals) recorded on the selected day
            var rawPayments = _context.Payments
                .AsNoTracking()
                .Where(p => p.CreationDate >= dayStart && p.CreationDate < dayEnd)
                .OrderBy(p => p.ReceiptNumber)
                .ToList();

            // Bursar options
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

            // Apply bursar filter
            var filteredPayments = string.IsNullOrWhiteSpace(Bursar)
                ? rawPayments
                : rawPayments.Where(p => p.CreatedByName == Bursar).ToList();

            // Fetch student names in one query
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

            // Build line items
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
                        Date = p.CreationDate,
                        StudentName = studentName,
                        Method = method.HasValue ? method.Value.ToDisplayName() : "—",
                        Amount = p.Amount ?? 0m,
                        CurrencyId = p.CurrencyId ?? BaseCurrency,
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

            // Totals — decimal math
            TotalPaymentsCount = Payments.Count;
            TotalReversalsCount = Reversals.Count;
            TotalCollected = Payments.Sum(l => l.BaseAmount);
            TotalReversed = Reversals.Sum(l => Math.Abs(l.BaseAmount));
            NetCollected = TotalCollected - TotalReversed;

            // Receipt range (payments only)
            if (Payments.Any())
            {
                FirstReceiptNumber = Payments.First().ReceiptNumber;
                LastReceiptNumber = Payments.Last().ReceiptNumber;
                CurrentReceiptYear = Payments.First().ReceiptYear;
            }

            // Method breakdown
            MethodGroups = Payments
                .GroupBy(l => l.Method)
                .Select(g => new MethodGroup
                {
                    MethodName = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.BaseAmount)
                })
                .OrderByDescending(g => g.Total)
                .ToList();

            GeneratedByName = User.Identity?.Name ?? "System";
            GeneratedOn = DateTime.Now;

            return Page();
        }
    }
}