using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using System.Security.Cryptography;
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
        private readonly IDataProtector _protector;

        public RecordPaymentModel(
            SMSDbContext context,
            ICurrentUserService currentUser,
            IPaymentService paymentService,
            IRbzRateService rbzService, IDataProtectionProvider protection)
        {
            _context = context;
            _currentUser = currentUser;
            _paymentService = paymentService;
            _rbzService = rbzService;
            _protector = protection.CreateProtector("School.PaymentConfirmation.v1");
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
        [BindProperty] public string SubmissionToken { get; set; } = "";
        [BindProperty] public string? ConfirmationToken { get; set; }
        public PaymentReview? Review { get; private set; }
        public record PaymentReview(RecordPaymentRequest Request, PaymentInput Entry, string Student, string Method, DateTime Expires);

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
            SubmissionToken = _protector.Protect($"submission|{_currentUser.UserId:N}|{ledgerId:N}|{Guid.NewGuid():N}");
            Input.CurrencyId = BaseCurrency;
            Input.ExchangeRate = 1m;
            // PaymentDate left null — the bursar must pick the date
            // from the slip. Do not default to today.

            return Page();
        }

        // =============================================================
        //  AJAX: fetch the RBZ rate for (currency, date)
        // =============================================================
        public async Task<JsonResult> OnGetFetchRateAsync(string currencyId, DateTime? paymentDate)
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments))
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Not authorized." });

            if (string.IsNullOrWhiteSpace(currencyId))
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Select a currency." });

            if (!paymentDate.HasValue || paymentDate.Value == default)
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Select a payment date." });

            var date = paymentDate.Value.Date;

            if (date > DateTime.Today)
                return new JsonResult(new RateFetchResponse { Success = false, Message = "Payment date cannot be in the future." });

            // Base currency? Rate is 1, no API call.
            var baseCurrency = await _context.Currencies
                .AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync();

            if (string.Equals(currencyId, baseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return new JsonResult(new RateFetchResponse
                {
                    Success = true,
                    Rate = 1m,
                    Source = "BASE",
                    ActualRateDate = date,
                    IsStale = false,
                    Message = "Base currency — no conversion needed."
                });
            }

            var result = await _rbzService.GetRateAsync(currencyId, date);

            if (result == null)
            {
                return new JsonResult(new RateFetchResponse
                {
                    Success = false,
                    Message = $"Could not fetch the RBZ rate for {currencyId} on {date:dd MMM yyyy}. " +
                              "Tick \"Override rate\" and enter the rate from your slip."
                });
            }

            var message = result.IsStale
                ? $"No RBZ rate published for {date:dd MMM yyyy}. " +
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
        public async Task<IActionResult> OnPostReviewAsync()
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments))
                return Forbid();

            var ok = await LoadAsync(Input.LedgerId);
            if (!ok) return NotFound();

            Guid submissionId;
            try
            {
                var parts = _protector.Unprotect(SubmissionToken).Split('|');
                if (parts.Length != 4 || parts[0] != "submission" || parts[1] != _currentUser.UserId?.ToString("N") || parts[2] != Input.LedgerId.ToString("N"))
                    throw new CryptographicException();
                submissionId = Guid.ParseExact(parts[3], "N");
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
            {
                ModelState.AddModelError(string.Empty, "The payment form has expired. Reload it and try again.");
                return Page();
            }

            if (!ModelState.IsValid)
                return Page();

            // [Required] on PaymentDate ensures it's non-null past ModelState.
            var paymentDate = Input.PaymentDate!.Value.Date;

            if (paymentDate > DateTime.Today)
            {
                ModelState.AddModelError("Input.PaymentDate",
                    "Payment date cannot be in the future.");
                return Page();
            }

            // ---------------------------------------------------------
            //  Determine the authoritative exchange rate, server-side.
            // ---------------------------------------------------------
            decimal authoritativeRate;
            string rateSource;

            if (Input.IsRateOverridden)
            {
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
                var rateResult = await _rbzService.GetRateAsync(Input.CurrencyId, paymentDate);

                if (rateResult == null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Could not fetch the RBZ rate for {Input.CurrencyId} on {paymentDate:dd MMM yyyy}. " +
                        "Tick \"Override rate\" and enter the rate from your slip, or try again later.");
                    return Page();
                }

                authoritativeRate = rateResult.Rate;
                rateSource = rateResult.Source;
            }

            try
            {
                var request = new RecordPaymentRequest
                {
                    SubmissionId = submissionId,
                    LedgerId = Input.LedgerId,
                    PaymentDate = paymentDate,
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
                };
                Review = new PaymentReview(request, Input, StudentName,
                    MethodOptions.FirstOrDefault(m => m.Id == request.PaymentMethodId)?.Label ?? "Unknown method", DateTime.UtcNow.AddMinutes(15));
                ConfirmationToken = _protector.Protect(JsonSerializer.Serialize(Review));
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync() => await ProcessConfirmationAsync(save: false);
        public async Task<IActionResult> OnPostConfirmAsync() => await ProcessConfirmationAsync(save: true);

        private async Task<IActionResult> ProcessConfirmationAsync(bool save)
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments)) return Forbid();
            PaymentReview review;
            try
            {
                review = JsonSerializer.Deserialize<PaymentReview>(_protector.Unprotect(ConfirmationToken ?? "")) ?? throw new CryptographicException();
                if (review.Request.UserId != _currentUser.UserId || review.Expires < DateTime.UtcNow)
                    throw new CryptographicException();
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
            {
                return BadRequest("Confirmation has expired. Reopen the payment form and review it again.");
            }
            ModelState.Clear();
            if (!await LoadAsync(review.Request.LedgerId)) return NotFound();
            if (!save)
            {
                Input = review.Entry;
                SubmissionToken = _protector.Protect($"submission|{_currentUser.UserId:N}|{Input.LedgerId:N}|{review.Request.SubmissionId:N}");
                return Page();
            }
            try
            {
                await _paymentService.RecordAsync(review.Request);
                TempData["PaymentSuccess"] = "Payment recorded successfully.";
                return RedirectToPage("./Ledger", new { id = review.Request.LedgerId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                Review = review;
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
