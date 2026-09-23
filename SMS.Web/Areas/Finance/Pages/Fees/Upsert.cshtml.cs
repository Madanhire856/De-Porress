using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class UpsertModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpsertModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // Lookups
        public List<SelectListItem> Grades { get; set; } = new();
        public List<SelectListItem> Terms { get; set; } = new();
        public List<SelectListItem> Currencies { get; set; } = new();
        public List<SelectListItem> LevyTypes { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool IsEditing => Input.Id.HasValue && Input.Id != Guid.Empty;

        public class InputModel
        {
            public Guid? Id { get; set; }

            [Required(ErrorMessage = "Select a grade.")]
            [Display(Name = "Grade")]
            public Guid GradeId { get; set; }

            [Required(ErrorMessage = "Select a term.")]
            [Display(Name = "Term")]
            public Guid TermId { get; set; }

            [Required(ErrorMessage = "Select a levy type.")]
            [Display(Name = "Levy Type")]
            public int LevyTypeId { get; set; }

            [Required(ErrorMessage = "Select a currency.")]
            [Display(Name = "Currency")]
            public string CurrencyId { get; set; } = "";

            [Required]
            [Range(0.01, 100_000_000, ErrorMessage = "Enter a valid amount greater than zero.")]
            [Display(Name = "Amount")]
            public decimal Amount { get; set; }
        }

        // =============================================================
        //  GET — show blank or pre-filled form
        // =============================================================
        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!_currentUser.HasRight(AccessRights.GenerateInvoices))
                return Forbid();

            await LoadLookupsAsync();

            if (id.HasValue)
            {
                var existing = await _context.FeesStructures
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == id.Value);

                if (existing == null) return NotFound();

                Input = new InputModel
                {
                    Id = existing.Id,
                    GradeId = existing.GradeId,
                    TermId = existing.TermId,
                    LevyTypeId = existing.LevyTypeId,
                    Amount = existing.Amount,
                    CurrencyId = existing.CurrencyId ?? ""
                };
            }

            return Page();
        }

        // =============================================================
        //  POST — create or update, then go to Details
        // =============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (!_currentUser.HasRight(AccessRights.GenerateInvoices))
                return Forbid();

            await LoadLookupsAsync();

            if (!ModelState.IsValid)
                return Page();

            // Duplicate guard: same grade + term + levy + currency
            var excludeId = Input.Id ?? Guid.Empty;
            var clash = await _context.FeesStructures
                .FirstOrDefaultAsync(f =>
                    f.GradeId == Input.GradeId &&
                    f.TermId == Input.TermId &&
                    f.LevyTypeId == Input.LevyTypeId &&
                    f.CurrencyId == Input.CurrencyId &&
                    f.Id != excludeId);

            if (clash != null)
            {
                ModelState.AddModelError(string.Empty,
                    "A fee for this grade, term, levy type, and currency already exists.");
                return Page();
            }

            Guid savedId;

            if (Input.Id.HasValue && Input.Id != Guid.Empty)
            {
                // ---- Update ----
                var entity = await _context.FeesStructures
                    .FirstOrDefaultAsync(f => f.Id == Input.Id.Value)
                    ?? throw new InvalidOperationException("Fee not found.");

                entity.GradeId = Input.GradeId;
                entity.TermId = Input.TermId;
                entity.LevyTypeId = Input.LevyTypeId;
                entity.Amount = Input.Amount;
                entity.CurrencyId = Input.CurrencyId;

                savedId = entity.Id;
                TempData["FeesSuccess"] = "Fee updated.";
            }
            else
            {
                // ---- Create ----
                var newEntity = new FeesStructure
                {
                    Id = Guid.NewGuid(),
                    GradeId = Input.GradeId,
                    TermId = Input.TermId,
                    LevyTypeId = Input.LevyTypeId,
                    Amount = Input.Amount,
                    CurrencyId = Input.CurrencyId,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };

                _context.FeesStructures.Add(newEntity);
                savedId = newEntity.Id;
                TempData["FeesSuccess"] = "Fee added.";
            }

            await _context.SaveChangesAsync();

            // ---- Redirect to the Details page with the saved record's ID ----
            return RedirectToPage("./FeesStructureDetails", new { id = savedId });
        }

        // =============================================================
        //  LOOKUPS
        // =============================================================
        private async Task LoadLookupsAsync()
        {
            Grades = await _context.Grades.AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name })
                .ToListAsync();

            var termRows = await _context.Terms.AsNoTracking()
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            Terms = termRows.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(t.Name)
                    ? $"{t.AcademicYear} · Term {t.Number}"
                    : t.Name
            }).ToList();

            Currencies = await _context.Currencies.AsNoTracking()
                .OrderByDescending(c => c.IsBase)
                .ThenBy(c => c.Code)
                .Select(c => new SelectListItem
                {
                    Value = c.Code,
                    Text = $"{c.Code} — {c.Name}"
                })
                .ToListAsync();

            LevyTypes = Enum.GetValues<LevyType>()
                .Select(l => new SelectListItem
                {
                    Value = ((int)l).ToString(),
                    Text = l.ToDisplayName()
                })
                .OrderBy(l => l.Text)
                .ToList();
        }
    }
}