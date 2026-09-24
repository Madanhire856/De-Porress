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
        private readonly IRbzRateService _rbzService;

        public RecordPaymentModel(
            SMSDbContext context,
            ICurrentUserService currentUser,
            IPaymentService paymentService,
            IRbzRateService rbzService)
        {
            _context = context;
            _currentUser = currentUser;
            _paymentService = paymentService;
            _rbzService = rbzService;
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

        public class CurrencyOption
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string Symbol { get; set; } = "";
            public bool IsBase { get; set; }
        }

        public class MethodOption
        {
            public int Id { get; set; }
            public string Label { get; set; } = "";
        }

        // Response shape for the AJAX rate fetch
        public class RateFetchResponse
        {
            public bool Success { get; set; }
            public decimal? Rate { get; set; }
            public string? Source { get; set; }
            public DateTime? ActualRateDate { get; set; }
            public bool IsStale { get; set; }
            public string? Message { get; set; }
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
            Input.PaymentDate = DateTime.Today;
            Input.CurrencyId = BaseCurrency;
            Input.ExchangeRate = 1m;

            return Page();
        }

        // =============================================================
        //  AJAX: fetch the RBZ rate for (currency, date)
        // =============================================================
        public async Task<JsonResult> OnGetFetchRateAsync(string currencyId, DateTime paymentDate)
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments))
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Not authorized." });

            if (string.IsNullOrWhiteSpace(currencyId))
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Select a currency." });

            if (paymentDate == default)
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Select a payment date." });

            if (paymentDate.Date > DateTime.Today)
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Payment date cannot be in the future." });

            // Base currency? Rate is 1, no API call.
            var baseCurrency = await _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            if (string.Equals(currencyId, baseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return new JsonResult(new RateFetchResponse
                {
                    Success = true,
                    Rate = 1m,
                    Source = "BASE",
                    ActualRateDate = paymentDate.Date,
                    IsStale = false,
                    Message = "Base currency — no conversion needed."
                });
            }

            // Foreign currency — ask the service
            var result = await _rbzService.GetRateAsync(currencyId, paymentDate.Date);

            if (result == null)
            {
                return new JsonResult(new RateFetchResponse
                {
                    Success = false,
                    Message = $"Could not fetch the RBZ rate for {currencyId} on {paymentDate:dd MMM yyyy}. " +
                              "Tick \"Override rate\" and enter the rate from your slip."
                });
            }

            var message = result.IsStale
                ? $"No RBZ rate published for {paymentDate:dd MMM yyyy}. " +
                  $"Using rate published {result.ActualRateDate:dd MMM yyyy}."
                : $"RBZ rate published {result.ActualRateDate:dd MMM yyyy}.";

            return new JsonResult(new RateFetchResponse
            {
                Success = true,
                Rate = result.Rate,
                Source = result.Source,
                ActualRateDate = result.ActualRateDate,
                IsStale = result.IsStale,
                Message = message
            });
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

            // ---------------------------------------------------------
            //  Determine the authoritative exchange rate, server-side.
            //  The client's posted rate is ignored for non-override posts.
            // ---------------------------------------------------------
            decimal authoritativeRate;
            string rateSource;

            if (Input.IsRateOverridden)
            {
                // Bursar explicitly overrode — validate and accept their rate.
                if (string.IsNullOrWhiteSpace(Input.RateOverrideReason))
                {
                    ModelState.AddModelError(
                        "Input.RateOverrideReason",
                        "Provide a reason for overriding the RBZ rate.");
                    return Page();
                }

                if (Input.ExchangeRate <= 0m)
                {
                    ModelState.AddModelError(
                        "Input.ExchangeRate",
                        "Enter the exchange rate from your slip.");
                    return Page();
                }

                authoritativeRate = Input.ExchangeRate;
                rateSource = "Manual";
            }
            else
            {
                // Normal path — fetch from RBZ (cache-first), ignore what the client sent.
                var rateResult = await _rbzService.GetRateAsync(Input.CurrencyId, Input.PaymentDate.Date);

                if (rateResult == null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Could not fetch the RBZ rate for {Input.CurrencyId} on {Input.PaymentDate:dd MMM yyyy}. " +
                        "Tick \"Override rate\" and enter the rate from your slip, or try again later.");
                    return Page();
                }

                authoritativeRate = rateResult.Rate;
                rateSource = rateResult.Source;
            }

            try
            {
                await _paymentService.RecordAsync(new RecordPaymentRequest
                {
                    LedgerId = Input.LedgerId,
                    PaymentDate = Input.PaymentDate.Date,
                    Amount = Input.Amount,
                    CurrencyId = Input.CurrencyId,
                    ExchangeRate = authoritativeRate,
                    RateSource = rateSource,
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

            if (ledger.Student.ClassId.HasValue)
            {
                var cls = await _context.Classes.AsNoTracking()
                    .Include(c => c.Grade)
                    .FirstOrDefaultAsync(c => c.Id == ledger.Student.ClassId.Value);
                GradeName = cls?.Grade?.Name ?? "—";
            }

            BaseCurrency = await _context.Currencies.AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            CurrencyOptions = await _context.Currencies.AsNoTracking()
                .OrderByDescending(c => c.IsBase)
                .ThenBy(c => c.Code)
                .Select(c => new CurrencyOption
                {
                    Code = c.Code,
                    Name = c.Name,
                    Symbol = c.Symbol,
                    IsBase = c.IsBase
                })
                .ToListAsync();

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