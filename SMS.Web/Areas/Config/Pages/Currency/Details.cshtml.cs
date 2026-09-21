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

        public IActionResult OnGet(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return RedirectToPage("./Index");

            CurrencyVM = _context.Currencies
                .Include(c => c.Creator)
                .FirstOrDefault(c => c.Code == code);

            if (CurrencyVM == null)
                return NotFound();

            CreatorEmail = CurrencyVM.Creator?.Email;

            return Page();
        }
    }
}