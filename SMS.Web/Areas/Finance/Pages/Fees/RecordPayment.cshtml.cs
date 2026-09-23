using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Finance.Pages.Fees.ViewModels;
using SMS.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class RecordPaymentModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPaymentService _paymentService;

        public RecordPaymentModel(
            SMSDbContext context,
            ICurrentUserService currentUser,
            IPaymentService paymentService)
        {
            _context = context;
            _currentUser = currentUser;
            _paymentService = paymentService;
        }

        // Header info
        public Guid LedgerId { get; set; }
        public string StudentName { get; set; } = "";
        public string TermName { get; set; } = "";
        public string GradeName { get; set; } = "—";
        public decimal CurrentBalance { get; set; }
        public string BaseCurrency { get; set; } = "USD";

        // Lookups
        public List<CurrencyOption> CurrencyOptions { get; set; } = new();
        public List<MethodOption> MethodOptions { get; set; } = new();

        [BindProperty]
        public PaymentInput Input { get; set; } = new();

        // ---- Page-specific DTOs ----
        public class CurrencyOption
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string Symbol { get; set; } = "";
            public bool IsBase { get; set; }
            public decimal ExchangeRateToBase { get; set; }
        }

        public class MethodOption
        {
            public int Id { get; set; }
            public string Label { get; set; } = "";
        }

        // =============================================================
        //  GET
        // =============================================================
        public async Task<IActionResult> OnGetAsync(Guid ledgerId)
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments))
                return Forbid();

            var ok = await LoadAsync(ledgerId);
            if (!ok) return NotFound();

            Input.LedgerId = ledgerId;
            Input.CurrencyId = BaseCurrency;
            Input.ExchangeRate = 1m;

            return Page();
        }

        // =============================================================
        //  POST
        // =============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments))
                return Forbid();

            var ok = await LoadAsync(Input.LedgerId);
            if (!ok) return NotFound();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                await _paymentService.RecordAsync(new RecordPaymentRequest
                {
                    LedgerId = Input.LedgerId,
                    Amount = Input.Amount,
                    CurrencyId = Input.CurrencyId,
                    ExchangeRate = Input.ExchangeRate,
                    RateSource = Input.RateSource,
                    PaymentMethodId = Input.PaymentMethodId,
                    ReferenceNumber = Input.ReferenceNumber,
                    ProofOfPaymentUrl = Input.ProofOfPaymentUrl,
                    UserId = _currentUser.UserId ?? Guid.Empty,
                    UserDisplayName = _currentUser.Name
                                     ?? User.Identity?.Name
                                     ?? "System"
                });

                TempData["PaymentSuccess"] = "Payment recorded successfully.";
                return RedirectToPage("./Ledger", new { id = Input.LedgerId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        // =============================================================
        //  LOAD
        // =============================================================
        private async Task<bool> LoadAsync(Guid ledgerId)
        {
            var ledger = await _context.StudentLedgers
                .AsNoTracking()
                .Include(l => l.Student)
                .Include(l => l.Term)
                .FirstOrDefaultAsync(l => l.Id == ledgerId);

            if (ledger == null) return false;

            LedgerId = ledger.Id;
            StudentName = $"{ledger.Student.Name} {ledger.Student.Surname}".Trim();
            TermName = string.IsNullOrWhiteSpace(ledger.Term?.Name)
                ? $"{ledger.Term?.AcademicYear} · Term {ledger.Term?.Number}"
                : ledger.Term!.Name;
            CurrentBalance = ledger.ClosingBalance;

            // Grade via class lookup
            if (ledger.Student.ClassId.HasValue)
            {
                var cls = await _context.Classes.AsNoTracking()
                    .Include(c => c.Grade)
                    .FirstOrDefaultAsync(c => c.Id == ledger.Student.ClassId.Value);
                GradeName = cls?.Grade?.Name ?? "—";
            }

            // Base currency
            BaseCurrency = await _context.Currencies.AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            // Currencies (base first, then alpha)
            CurrencyOptions = await _context.Currencies.AsNoTracking()
                .OrderByDescending(c => c.IsBase)
                .ThenBy(c => c.Code)
                .Select(c => new CurrencyOption
                {
                    Code = c.Code,
                    Name = c.Name,
                    Symbol = c.Symbol,
                    IsBase = c.IsBase ?? false,
                    ExchangeRateToBase = c.ExchangeRateToBase ?? 1m
                })
                .ToListAsync();

            // Payment methods from the enum
            MethodOptions = Enum.GetValues<PaymentMethod>()
                .Select(m => new MethodOption
                {
                    Id = (int)m,
                    Label = m.ToDisplayName()
                })
                .OrderBy(m => m.Label)
                .ToList();

            return true;
        }
    }
}