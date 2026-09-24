using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Currency
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;

        public DetailsModel(SMSDbContext context)
        {
            _context = context;
        }

        public Data.Currency CurrencyVM { get; set; } = null!;
        public string? CreatorEmail { get; set; }

        // ---- Exposed for the view ----
        public bool IsBase => CurrencyVM.IsBase == true;
        public decimal? ExchangeRateToBase => CurrencyVM.ExchangeRateToBase;
        public string BaseCurrencyCode { get; set; } = "—";

        public IActionResult OnGet(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return RedirectToPage("./Index");

            CurrencyVM = _context.Currencies
                .AsNoTracking()
                .Include(c => c.Creator)
                .FirstOrDefault(c => c.Code == code);

            if (CurrencyVM == null)
                return NotFound();

            CreatorEmail = CurrencyVM.Creator?.Email;

            if (!IsBase)
            {
                BaseCurrencyCode = _context.Currencies
                    .AsNoTracking()
                    .Where(c => c.IsBase == true)
                    .Select(c => c.Code)
                    .FirstOrDefault() ?? "USD";
            }

            return Page();
        }
    }
}