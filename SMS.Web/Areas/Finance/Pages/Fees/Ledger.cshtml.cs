using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class LedgerModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LedgerModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public Guid LedgerId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string GradeName { get; set; } = "—";
        public string TermName { get; set; } = "—";
        public string BaseCurrency { get; set; } = "USD";

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalCharged { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalReversed { get; set; }

        public int StatusId { get; set; }
        public string StatusLabel { get; set; } = "—";
        public string StatusClass { get; set; } = "";

        public List<PaymentItem> Payments { get; set; } = new();
        public List<CorrectionItem> Corrections { get; set; } = new();

        public class PaymentItem
        {
            public Guid Id { get; set; }
            public int ReceiptNumber { get; set; }
            public int ReceiptYear { get; set; }
            public string ReceiptDisplay => $"RCP-{ReceiptYear}-{ReceiptNumber:D5}";
            public DateTime Date { get; set; }
            public string Method { get; set; } = "";
            public decimal Amount { get; set; }
            public string CurrencyId { get; set; } = "";
            public decimal BaseAmount { get; set; }
            public string? Reference { get; set; }
            public string CreatedByName { get; set; } = "";
            public bool IsReversal { get; set; }
            public Guid? ReversesPaymentId { get; set; }
            public string? ReversalReason { get; set; }
            public string? CorrectionReason { get; set; }
            public bool AlreadyReversed { get; set; }
            public string? ReversalReceiptDisplay { get; set; }
            public string? ReversedPaymentReceiptDisplay { get; set; }
            public DateTime? ReversedOn { get; set; }
            public string? ReversedByName { get; set; }
        }

        public class CorrectionItem
        {
            public string ReceiptDisplay { get; set; } = "";
            public string CorrectedByName { get; set; } = "";
            public DateTime CorrectedOn { get; set; }
            public string Reason { get; set; } = "";
            public decimal PreviousAmount { get; set; }
            public decimal CorrectedAmount { get; set; }
            public decimal PreviousBaseAmount { get; set; }
            public decimal CorrectedBaseAmount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            var ledger = await _context.StudentLedgers
                .AsNoTracking()
                .Include(l => l.Student)
                .Include(l => l.Term)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (ledger == null) return NotFound();

            LedgerId = ledger.Id;
            StudentId = ledger.StudentId;
            StudentName = $"{ledger.Student.Name} {ledger.Student.Surname}".Trim();
            TermName = string.IsNullOrWhiteSpace(ledger.Term?.Name)
                ? $"{ledger.Term?.AcademicYear} · Term {ledger.Term?.Number}"
                : ledger.Term!.Name;

            OpeningBalance = ledger.OpeningBalance;
            ClosingBalance = ledger.ClosingBalance;
            TotalCharged = ledger.OpeningBalance;
            StatusId = ledger.StatusId ?? 0;

            (StatusLabel, StatusClass) = StatusId switch
            {
                (int)StudentLedgerStatus.SETTLED => ("Settled", "status-active"),
                (int)StudentLedgerStatus.PARTIALLY_SETTLED => ("Partial", "status-count"),
                (int)StudentLedgerStatus.OVER_DUE => ("Overdue", "status-overdue"),
                _ => ("—", "status-count")
            };

            // Grade via class lookup
            if (ledger.Student.ClassId.HasValue)
            {
                var cls = await _context.Classes.AsNoTracking()
                    .Include(c => c.Grade)
                    .FirstOrDefaultAsync(c => c.Id == ledger.Student.ClassId.Value);
                GradeName = cls?.Grade?.Name ?? "—";
            }

            BaseCurrency = await _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            var paymentRows = await _context.Payments
                .AsNoTracking()
                .Where(p => p.LedgerId == id)
                .OrderByDescending(p => p.CreationDate)
                .ToListAsync();

            // IDs that have been reversed (the compensation points back at them)
            var reversedIds = paymentRows
                .Where(p => p.ReversesPaymentId.HasValue)
                .Select(p => p.ReversesPaymentId!.Value)
                .ToHashSet();

            var paymentsById = paymentRows.ToDictionary(p => p.Id);
            var reversalsByOriginalId = paymentRows
                .Where(p => p.IsReversal && p.ReversesPaymentId.HasValue)
                .GroupBy(p => p.ReversesPaymentId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            Payments = paymentRows.Select(p =>
            {
                var method = EnumExtensions.ParsePaymentMethod(p.PaymentMethodId);
                reversalsByOriginalId.TryGetValue(p.Id, out var linkedReversal);
                paymentsById.TryGetValue(p.ReversesPaymentId ?? Guid.Empty, out var reversedPayment);
                return new PaymentItem
                {
                    Id = p.Id,
                    ReceiptNumber = p.ReceiptNumber,
                    ReceiptYear = p.ReceiptYear,
                    Date = p.CreationDate,
                    Method = method.HasValue ? method.Value.ToDisplayName() : "—",
                    Amount = p.Amount ?? 0m,
                    CurrencyId = p.CurrencyId ?? BaseCurrency,
                    BaseAmount = p.BaseAmount ?? p.Amount ?? 0m,
                    Reference = p.ReferenceNumber,
                    CreatedByName = p.CreatedByName ?? "",
                    IsReversal = p.IsReversal,
                    ReversesPaymentId = p.ReversesPaymentId,
                    ReversalReason = p.ReversalReason,
                    CorrectionReason = p.CorrectionReason,
                    AlreadyReversed = reversedIds.Contains(p.Id),
                    ReversalReceiptDisplay = linkedReversal == null
                        ? null
                        : $"RCP-{linkedReversal.ReceiptYear}-{linkedReversal.ReceiptNumber:D5}",
                    ReversedPaymentReceiptDisplay = reversedPayment == null
                        ? null
                        : $"RCP-{reversedPayment.ReceiptYear}-{reversedPayment.ReceiptNumber:D5}",
                    ReversedOn = p.ReversedOn,
                    ReversedByName = p.IsReversal ? p.CreatedByName : null
                };
            }).ToList();

            TotalPaid = Payments.Where(p => !p.IsReversal).Sum(p => p.BaseAmount);
            TotalReversed = Payments.Where(p => p.IsReversal).Sum(p => Math.Abs(p.BaseAmount));

            if (_currentUser.HasRight(AccessRights.ReversePayments))
            {
                Corrections = await _context.PaymentCorrections
                    .AsNoTracking()
                    .Where(c => c.Payment.LedgerId == id)
                    .OrderByDescending(c => c.CorrectedOn)
                    .Select(c => new CorrectionItem
                    {
                        ReceiptDisplay = $"RCP-{c.Payment.ReceiptYear}-{c.Payment.ReceiptNumber:D5}",
                        CorrectedByName = c.CorrectedByName,
                        CorrectedOn = c.CorrectedOn,
                        Reason = c.Reason,
                        PreviousAmount = c.PreviousAmount,
                        CorrectedAmount = c.CorrectedAmount,
                        PreviousBaseAmount = c.PreviousBaseAmount,
                        CorrectedBaseAmount = c.CorrectedBaseAmount
                    })
                    .ToListAsync();
            }

            return Page();
        }
    }
}
