using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class ReversePaymentModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPaymentService _paymentService;

        public ReversePaymentModel(
            SMSDbContext context,
            ICurrentUserService currentUser,
            IPaymentService paymentService)
        {
            _context = context;
            _currentUser = currentUser;
            _paymentService = paymentService;
        }

        public PaymentVM? Payment { get; set; }
        public string StudentName { get; set; } = "";
        public string TermName { get; set; } = "";
        public Guid LedgerId { get; set; }
        public string BaseCurrency { get; set; } = "USD";

        [BindProperty]
        public ReverseInput Input { get; set; } = new();

        public class PaymentVM
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
            public bool AlreadyReversed { get; set; }
            public bool IsReversal { get; set; }
        }

        public class ReverseInput
        {
            public Guid PaymentId { get; set; }

            [Required(ErrorMessage = "A reason is required.")]
            [StringLength(500, MinimumLength = 10,
                ErrorMessage = "Reason must be at least 10 characters.")]
            [Display(Name = "Reason")]
            public string Reason { get; set; } = "";
        }

        // =============================================================
        //  GET
        // =============================================================
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments))
                return Forbid();

            var ok = await LoadAsync(id);
            if (!ok) return NotFound();

            // Guard: can't reverse a reversal, can't re-reverse
            if (Payment!.IsReversal)
            {
                TempData["PaymentError"] = "Cannot reverse a reversal.";
                return RedirectToPage("./Ledger", new { id = LedgerId });
            }

            if (Payment.AlreadyReversed)
            {
                TempData["PaymentError"] = "This payment has already been reversed.";
                return RedirectToPage("./Ledger", new { id = LedgerId });
            }

            Input.PaymentId = id;
            return Page();
        }

        // =============================================================
        //  POST
        // =============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (!_currentUser.HasRight(AccessRights.RecordPayments))
                return Forbid();

            var ok = await LoadAsync(Input.PaymentId);
            if (!ok) return NotFound();

            // Re-check the state guards even on POST — user may have posted stale data
            if (Payment!.IsReversal)
            {
                ModelState.AddModelError(string.Empty, "Cannot reverse a reversal.");
                return Page();
            }

            if (Payment.AlreadyReversed)
            {
                ModelState.AddModelError(string.Empty, "This payment has already been reversed.");
                return Page();
            }

            if (!ModelState.IsValid)
                return Page();

            try
            {
                await _paymentService.ReverseAsync(new ReversePaymentRequest
                {
                    OriginalPaymentId = Input.PaymentId,
                    Reason = Input.Reason,
                    UserId = _currentUser.UserId ?? Guid.Empty,
                    UserDisplayName = _currentUser.Name
                                     ?? User.Identity?.Name
                                     ?? "System"
                });

                TempData["PaymentSuccess"] = "Payment reversed successfully.";
                return RedirectToPage("./Ledger", new { id = LedgerId });
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
        private async Task<bool> LoadAsync(Guid paymentId)
        {
            var p = await _context.Payments
                .AsNoTracking()
                .Include(x => x.Ledger)
                    .ThenInclude(l => l!.Student)
                .Include(x => x.Ledger)
                    .ThenInclude(l => l!.Term)
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (p == null || p.Ledger == null) return false;

            var method = EnumExtensions.ParsePaymentMethod(p.PaymentMethodId);

            // Has this payment already been reversed? (a reversal row points back at it)
            var alreadyReversed = await _context.Payments
                .AsNoTracking()
                .AnyAsync(x => x.ReversesPaymentId == p.Id);

            Payment = new PaymentVM
            {
                Id = p.Id,
                ReceiptNumber = p.ReceiptNumber,
                ReceiptYear = p.ReceiptYear,
                Date = p.CreationDate,
                Method = method.HasValue ? method.Value.ToDisplayName() : "—",
                Amount = p.Amount ?? 0m,
                CurrencyId = p.CurrencyId ?? "USD",
                BaseAmount = p.BaseAmount ?? p.Amount ?? 0m,
                Reference = p.ReferenceNumber,
                CreatedByName = p.CreatedByName ?? "",
                AlreadyReversed = alreadyReversed,
                IsReversal = p.IsReversal
            };

            StudentName = $"{p.Ledger.Student.Name} {p.Ledger.Student.Surname}".Trim();
            TermName = string.IsNullOrWhiteSpace(p.Ledger.Term?.Name)
                ? $"{p.Ledger.Term?.AcademicYear} · Term {p.Ledger.Term?.Number}"
                : p.Ledger.Term!.Name;
            LedgerId = p.Ledger.Id;

            BaseCurrency = await _context.Currencies.AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            return true;
        }
    }
}