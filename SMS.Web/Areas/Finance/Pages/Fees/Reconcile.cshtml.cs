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
    public class ReconcileModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReconcileModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public List<BankSide> UnmatchedBankEntries { get; set; } = new();
        public List<PaymentSide> UnmatchedPayments { get; set; } = new();
        public List<MatchedPair> Matched { get; set; } = new();

        public int UnmatchedBankCount => UnmatchedBankEntries.Count;
        public int UnmatchedPaymentCount => UnmatchedPayments.Count;
        public int MatchedCount => Matched.Count;

        public string BaseCurrency { get; set; } = "USD";

        public class BankSide
        {
            public Guid Id { get; set; }
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }         // ← was int
            public string? Description { get; set; }
            public string? Reference { get; set; }
        }

        public class PaymentSide
        {
            public Guid Id { get; set; }
            public int ReceiptNumber { get; set; }
            public int ReceiptYear { get; set; }
            public string ReceiptDisplay => $"RCP-{ReceiptYear}-{ReceiptNumber:D5}";
            public DateTime Date { get; set; }
            public decimal BaseAmount { get; set; }     // ← was int
            public string Method { get; set; } = "";
            public string StudentName { get; set; } = "";
            public string? Reference { get; set; }
        }

        public class MatchedPair
        {
            public Guid BankEntryId { get; set; }
            public DateTime BankDate { get; set; }
            public decimal BankAmount { get; set; }     // ← was int
            public string? BankReference { get; set; }
            public string ReceiptDisplay { get; set; } = "";
            public string StudentName { get; set; } = "";
            public decimal PaymentAmount { get; set; }  // ← was int
            public DateTime MatchedOn { get; set; }
            public string MatchedByName { get; set; } = "";
        }

        // =============================================================
        //  GET
        // =============================================================
        public async Task<IActionResult> OnGetAsync()
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            await LoadAsync();
            return Page();
        }

        // =============================================================
        //  POST — match a bank line to a payment
        // =============================================================
        public async Task<IActionResult> OnPostMatchAsync(Guid bankEntryId, Guid paymentId)
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            if (paymentId == Guid.Empty)
            {
                TempData["RecError"] = "Select a payment on the right before matching.";
                return RedirectToPage();
            }

            var entry = await _context.BankStatementEntries
                .FirstOrDefaultAsync(e => e.Id == bankEntryId);

            if (entry == null)
            {
                TempData["RecError"] = "Bank entry not found.";
                return RedirectToPage();
            }

            if (entry.IsMatched)
            {
                TempData["RecError"] = "That bank entry is already matched.";
                return RedirectToPage();
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                TempData["RecError"] = "Payment not found.";
                return RedirectToPage();
            }

            var alreadyMatched = await _context.BankStatementEntries
                .AnyAsync(e => e.MatchedPaymentId == paymentId);

            if (alreadyMatched)
            {
                TempData["RecError"] = "That payment is already matched to another statement line.";
                return RedirectToPage();
            }

            // ---- Amount comparison, now decimal vs decimal ----
            var paymentBase = payment.BaseAmount ?? payment.Amount ?? 0m;
            var entryAmount = entry.Amount ?? 0m;

            if (paymentBase != entryAmount)
            {
                var variance = paymentBase - entryAmount;
                TempData["RecWarning"] =
                    $"Matched with a variance: statement {entryAmount:N2} vs payment {paymentBase:N2} " +
                    $"(difference {variance:N2}).";
            }

            entry.IsMatched = true;
            entry.MatchedPaymentId = paymentId;
            entry.MatchedOn = DateTime.Now;
            entry.MatchedByUserId = _currentUser.UserId;

            await _context.SaveChangesAsync();

            TempData["RecSuccess"] = "Matched successfully.";
            return RedirectToPage();
        }

        // =============================================================
        //  POST — remove a match
        // =============================================================
        public async Task<IActionResult> OnPostUnmatchAsync(Guid bankEntryId)
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            var entry = await _context.BankStatementEntries
                .FirstOrDefaultAsync(e => e.Id == bankEntryId);

            if (entry == null) return RedirectToPage();

            entry.IsMatched = false;
            entry.MatchedPaymentId = null;
            entry.MatchedOn = null;
            entry.MatchedByUserId = null;

            await _context.SaveChangesAsync();

            TempData["RecSuccess"] = "Match removed.";
            return RedirectToPage();
        }

        // =============================================================
        //  LOAD
        // =============================================================
        private async Task LoadAsync()
        {
            // ---- Base currency (nullable IsBase, Code is PK) ----
            BaseCurrency = await _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            // ---- Unmatched bank entries ----
            UnmatchedBankEntries = await _context.BankStatementEntries
                .AsNoTracking()
                .Where(e => !e.IsMatched)
                .OrderBy(e => e.EntryDate)
                .Select(e => new BankSide
                {
                    Id = e.Id,
                    Date = e.EntryDate,
                    Amount = e.Amount ?? 0m,
                    Description = e.Description,
                    Reference = e.BankReference
                })
                .ToListAsync();

            // ---- Unmatched payments ----
            var matchedPaymentIds = await _context.BankStatementEntries
                .Where(e => e.MatchedPaymentId != null)
                .Select(e => e.MatchedPaymentId!.Value)
                .ToListAsync();

            var unmatchedPaymentRows = await _context.Payments
                .AsNoTracking()
                .Where(p => !p.IsReversal)
                .Where(p => p.LedgerId != null)
                .Where(p => !matchedPaymentIds.Contains(p.Id))
                .Include(p => p.Ledger)
                    .ThenInclude(l => l!.Student)
                .OrderByDescending(p => p.CreationDate)
                .Take(200)
                .ToListAsync();

            UnmatchedPayments = unmatchedPaymentRows
                .Select(p =>
                {
                    var method = EnumExtensions.ParsePaymentMethod(p.PaymentMethodId);
                    return new PaymentSide
                    {
                        Id = p.Id,
                        ReceiptNumber = p.ReceiptNumber,
                        ReceiptYear = p.ReceiptYear,
                        Date = p.CreationDate,
                        BaseAmount = p.BaseAmount ?? p.Amount ?? 0m,
                        Method = method.HasValue ? method.Value.ToDisplayName() : "—",
                        StudentName = p.Ledger?.Student != null
                            ? $"{p.Ledger.Student.Name} {p.Ledger.Student.Surname}".Trim()
                            : "—",
                        Reference = p.ReferenceNumber
                    };
                })
                .ToList();

            // ---- Matched pairs ----
            var matchedRows = await _context.BankStatementEntries
                .AsNoTracking()
                .Include(e => e.MatchedPayment)
                    .ThenInclude(p => p!.Ledger)
                        .ThenInclude(l => l!.Student)
                .Include(e => e.MatchedByUser)
                .Where(e => e.IsMatched && e.MatchedPaymentId != null)
                .OrderByDescending(e => e.MatchedOn)
                .Take(200)
                .ToListAsync();

            Matched = matchedRows
                .Where(e => e.MatchedPayment != null)
                .Select(e => new MatchedPair
                {
                    BankEntryId = e.Id,
                    BankDate = e.EntryDate,
                    BankAmount = e.Amount ?? 0m,
                    BankReference = e.BankReference,
                    ReceiptDisplay = $"RCP-{e.MatchedPayment!.ReceiptYear}-{e.MatchedPayment.ReceiptNumber:D5}",
                    StudentName = e.MatchedPayment.Ledger?.Student != null
                        ? $"{e.MatchedPayment.Ledger.Student.Name} {e.MatchedPayment.Ledger.Student.Surname}".Trim()
                        : "—",
                    PaymentAmount = e.MatchedPayment.BaseAmount ?? e.MatchedPayment.Amount ?? 0m,
                    MatchedOn = e.MatchedOn ?? DateTime.MinValue,
                    MatchedByName = e.MatchedByUser?.Name ?? "—"
                })
                .ToList();
        }
    }
}