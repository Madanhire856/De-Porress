using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Currency.ViewModels;
using System;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Currency
{
    [Authorize]
    public class UpsertModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpsertModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [BindProperty]
        public CurrencyVM currency { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? code)
        {
            // ---- Rights guard ----
            if (string.IsNullOrEmpty(code) && !_currentUser.HasRight(AccessRights.CreateCurrencies))
                return Forbid();
            if (!string.IsNullOrEmpty(code) && !_currentUser.HasRight(AccessRights.EditCurrencies))
                return Forbid();

            if (!string.IsNullOrEmpty(code))
            {
                // === EDIT ===
                var entity = await _context.Currencies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (entity != null)
                {
                    currency = new CurrencyVM
                    {
                        Code = entity.Code,
                        Name = entity.Name,
                        Symbol = entity.Symbol
                    };
                }
            }
            // else: CREATE mode — CurrencyVM already initialized with defaults

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? code)
        {
            // ---- Same guard on POST ----
            if (string.IsNullOrEmpty(code) && !_currentUser.HasRight(AccessRights.CreateCurrencies))
                return Forbid();
            if (!string.IsNullOrEmpty(code) && !_currentUser.HasRight(AccessRights.EditCurrencies))
                return Forbid();

            if (!ModelState.IsValid)
                return Page();

            Data.Currency entity;

            if (string.IsNullOrEmpty(code))
            {
                // === CREATE ===
                // Uniqueness — Code
                var clash = await _context.Currencies
                    .FirstOrDefaultAsync(c => c.Code == currency.Code);

                if (clash != null)
                {
                    ModelState.AddModelError("currency.Code", "A currency with this code already exists.");
                    return Page();
                }

                entity = new Data.Currency
                {
                    Code = currency.Code,
                    Name = currency.Name,
                    Symbol = currency.Symbol,
                    CreatorId = _currentUser.UserId,
                    CreationDate = DateTime.Now
                };
                await _context.Currencies.AddAsync(entity);
            }
            else
            {
                // === EDIT ===
                entity = await _context.Currencies
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (entity == null)
                    return NotFound();

                entity.Name = currency.Name;
                entity.Symbol = currency.Symbol;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", code = entity.Code });
        }
    }
}