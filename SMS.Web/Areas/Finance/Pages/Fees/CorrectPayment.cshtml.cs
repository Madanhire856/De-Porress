using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Finance.Pages.Fees
{
    [Authorize]
    public class CorrectPaymentModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPaymentService _paymentService;

        public CorrectPaymentModel(SMSDbContext context, ICurrentUserService currentUser, IPaymentService paymentService)
        {
            _context = context;
            _currentUser = currentUser;
            _paymentService = paymentService;
        }

        public Guid LedgerId { get; set; }
        public string StudentName { get; set; } = "";
        public string BaseCurrency { get; set; } = "USD";
        public string ReceiptDisplay { get; set; } = "";
        public List<MethodOption> MethodOptions { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class MethodOption { public int Id { get; set; } public string Label { get; set; } = ""; }

        public class InputModel
        {
            public Guid PaymentId { get; set; }
            [Required] public DateTime? PaymentDate { get; set; }
            [Range(0.01, 100_000_000)] public decimal Amount { get; set; }
            [Range(1, int.MaxValue)] public int PaymentMethodId { get; set; }
            [StringLength(50)] public string? ReferenceNumber { get; set; }
            [StringLength(500)] public string? ProofOfPaymentUrl { get; set; }
            [Required, StringLength(500, MinimumLength = 10)] public string CorrectionReason { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (!_currentUser.HasRight(AccessRights.ReversePayments)) return Forbid();
            if (!await LoadAsync(id)) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!_currentUser.HasRight(AccessRights.ReversePayments)) return Forbid();
            if (!await LoadAsync(Input.PaymentId, preserveInput: true)) return NotFound();
            if (!ModelState.IsValid) return Page();

            try
            {
                await _paymentService.CorrectAsync(new CorrectPaymentRequest
                {
                    PaymentId = Input.PaymentId,
                    PaymentDate = Input.PaymentDate!.Value,
                    Amount = Input.Amount,
                    PaymentMethodId = Input.PaymentMethodId,
                    ReferenceNumber = Input.ReferenceNumber,
                    ProofOfPaymentUrl = Input.ProofOfPaymentUrl,
                    CorrectionReason = Input.CorrectionReason,
                    UserId = _currentUser.UserId ?? Guid.Empty,
                    UserDisplayName = _currentUser.Name ?? User.Identity?.Name ?? "System"
                });
                TempData["PaymentSuccess"] = "Payment corrected. The previous and corrected values were recorded in the audit log.";
                return RedirectToPage("./Ledger", new { id = LedgerId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        private async Task<bool> LoadAsync(Guid paymentId, bool preserveInput = false)
        {
            var payment = await _context.Payments.AsNoTracking()
                .Include(p => p.Ledger).ThenInclude(l => l!.Student)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment?.Ledger == null || payment.IsReversal) return false;

            var alreadyReversed = await _context.Payments.AsNoTracking()
                .AnyAsync(p => p.ReversesPaymentId == payment.Id);
            if (alreadyReversed) return false;

            LedgerId = payment.Ledger.Id;
            StudentName = $"{payment.Ledger.Student.Name} {payment.Ledger.Student.Surname}".Trim();
            ReceiptDisplay = $"RCP-{payment.ReceiptYear}-{payment.ReceiptNumber:D5}";
            BaseCurrency = await _context.Currencies.AsNoTracking().Where(c => c.IsBase).Select(c => c.Code).FirstOrDefaultAsync() ?? "USD";
            MethodOptions = Enum.GetValues<PaymentMethod>().Select(m => new MethodOption { Id = (int)m, Label = m.ToDisplayName() }).OrderBy(m => m.Label).ToList();

            if (!preserveInput)
            {
                Input = new InputModel
                {
                    PaymentId = payment.Id,
                    PaymentDate = payment.PaymentDate,
                    Amount = payment.Amount ?? 0m,
                    PaymentMethodId = payment.PaymentMethodId,
                    ReferenceNumber = payment.ReferenceNumber,
                    ProofOfPaymentUrl = payment.ProofOfPaymentUrl
                };
            }
            return true;
        }
    }
}
