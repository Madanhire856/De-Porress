using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Currency.ViewModels;
using System;
using System.Linq;
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

        // =============================================================
        //  GET
        // =============================================================
        public async Task<IActionResult> OnGetAsync(string? code)
        {
            if (string.IsNullOrEmpty(code) && !_currentUser.HasRight(AccessRights.CreateCurrencies))
                return Forbid();
            if (!string.IsNullOrEmpty(code) && !_currentUser.HasRight(AccessRights.EditCurrencies))
                return Forbid();

            if (!string.IsNullOrEmpty(code))
            {
                var entity = await _context.Currencies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (entity != null)
                {
                    currency = new CurrencyVM
                    {
                        Code = entity.Code,
                        Name = entity.Name,
                        Symbol = entity.Symbol,
                        IsBase = entity.IsBase 
                    };
                }
            }

            return Page();
        }

        // =============================================================
        //  POST
        // =============================================================
        public async Task<IActionResult> OnPostAsync(string? code)
        {
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
                    IsBase = false,    
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

            // =========================================================
            //  BASE CURRENCY HANDLING
            // =========================================================
            var wasBase = entity.IsBase == true;

            if (currency.IsBase == true)
            {
                // ---- Setting this currency as base ----
                // Clear any other currency's base flag BEFORE setting this one.
                // This avoids a unique-index collision on UX_Currency_IsBase.
                await _context.Currencies
                    .Where(c => c.IsBase == true && c.Code != entity.Code)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBase, false));

                entity.IsBase = true;
                entity.ExchangeRateToBase = 1.0m;
            }
            else
            {
                // ---- User un-checked IsBase ----
                // If this was the base currency, refuse unless another base exists.
                if (wasBase)
                {
                    var anotherBaseExists = await _context.Currencies
                        .AnyAsync(c => c.IsBase == true && c.Code != entity.Code);

                    if (!anotherBaseExists)
                    {
                        ModelState.AddModelError(string.Empty,
                            "At least one currency must be marked as the base currency. " +
                            "Set another currency as base first, then uncheck this one.");
                        return Page();
                    }

                    entity.IsBase = false;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", code = entity.Code });
        }
    }
}