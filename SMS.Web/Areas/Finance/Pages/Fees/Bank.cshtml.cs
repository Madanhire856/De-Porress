using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class BankModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public BankModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public List<BankRow> Entries { get; set; } = new();

        [BindProperty]
        public EntryInput Input { get; set; } = new();

        public class BankRow
        {
            public Guid Id { get; set; }
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }         // ← was int
            public string? Description { get; set; }
            public string? Reference { get; set; }
            public string Currency { get; set; } = "USD";
            public bool IsMatched { get; set; }
            public string? MatchedReceipt { get; set; }
            public string? MatchedStudent { get; set; }
        }

        public class EntryInput
        {
            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Date")]
            public DateTime EntryDate { get; set; } = DateTime.Today;

            [Required]
            [Range(0.01, 100_000_000, ErrorMessage = "Enter a valid amount.")]
            [Display(Name = "Amount")]
            public decimal Amount { get; set; }          // ← was int, now supports cents

            [StringLength(200)]
            [Display(Name = "Description")]
            public string? Description { get; set; }

            [StringLength(100)]
            [Display(Name = "Bank Reference")]
            public string? BankReference { get; set; }

            [StringLength(10)]
            [Display(Name = "Currency")]
            public string? CurrencyId { get; set; }
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
        //  POST — add a statement line
        // =============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            // Default currency to base if not specified
            var currencyId = Input.CurrencyId;
            if (string.IsNullOrWhiteSpace(currencyId))
            {
                currencyId = await _context.Currencies
                    .Where(c => c.IsBase == true)
                    .Select(c => c.Code)
                    .FirstOrDefaultAsync() ?? "USD";
            }

            _context.BankStatementEntries.Add(new BankStatementEntry
            {
                Id = Guid.NewGuid(),
                EntryDate = Input.EntryDate.Date,
                Amount = Input.Amount,                  // decimal → decimal? implicit
                Description = Trim(Input.Description),
                BankReference = Trim(Input.BankReference),
                CurrencyId = currencyId,
                IsMatched = false,
                CreationDate = DateTime.Now,
                CreatorId = (Guid)_currentUser.UserId
            });

            await _context.SaveChangesAsync();

            TempData["BankSuccess"] = "Bank statement entry recorded.";
            return RedirectToPage("./Bank");
        }

        // =============================================================
        //  POST — delete an unmatched line
        // =============================================================
        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            if (!_currentUser.HasRight(AccessRights.ViewFeeBalances))
                return Forbid();

            var entry = await _context.BankStatementEntries.FindAsync(id);
            if (entry == null) return NotFound();

            if (entry.IsMatched)
            {
                TempData["BankError"] = "Cannot delete a matched entry. Unmatch it first.";
                return RedirectToPage("./Bank");
            }

            _context.BankStatementEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Bank");
        }

        // =============================================================
        //  LOAD
        // =============================================================
        private async Task LoadAsync()
        {
            var baseCurrency = await _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            var rows = await _context.BankStatementEntries
                .AsNoTracking()
                .Include(e => e.MatchedPayment)
                    .ThenInclude(p => p!.Ledger)
                        .ThenInclude(l => l!.Student)
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.CreationDate)
                .Take(200)
                .ToListAsync();

            Entries = rows.Select(e => new BankRow
            {
                Id = e.Id,
                Date = e.EntryDate,
                Amount = e.Amount ?? 0m,       
                Description = e.Description,
                Reference = e.BankReference,
                Currency = e.CurrencyId ?? baseCurrency,
                IsMatched = e.IsMatched,
                MatchedReceipt = e.MatchedPayment != null
                    ? $"RCP-{e.MatchedPayment.ReceiptYear}-{e.MatchedPayment.ReceiptNumber:D5}"
                    : null,
                MatchedStudent = e.MatchedPayment?.Ledger?.Student != null
                    ? $"{e.MatchedPayment.Ledger.Student.Name} {e.MatchedPayment.Ledger.Student.Surname}".Trim()
                    : null
            }).ToList();
        }

        private static string? Trim(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}