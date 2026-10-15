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
        public List<SelectListItem> GradeLevels { get; set; } = new();
        public List<SelectListItem> Terms { get; set; } = new();
        public List<SelectListItem> LevyTypes { get; set; } = new();

        /// <summary>The base currency — every fee is quoted in this.</summary>
        public string BaseCurrency { get; set; } = "USD";

        /// <summary>Total number of grades in the system — used for the "Everyone" label.</summary>
        public int TotalGradeCount { get; set; }

        /// <summary>Grade-level ID → number of grades in that level.</summary>
        public Dictionary<int, int> GradeLevelCounts { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool IsEditing => Input.Id.HasValue && Input.Id != Guid.Empty;

        public enum ApplyScope
        {
            SingleGrade = 0,
            GradeLevel = 1,
            Everyone = 2
        }

        public class InputModel
        {
            public Guid? Id { get; set; }

            [Display(Name = "Apply to")]
            public ApplyScope Scope { get; set; } = ApplyScope.SingleGrade;

            /// <summary>Required when Scope = SingleGrade.</summary>
            [Display(Name = "Grade")]
            public Guid? GradeId { get; set; }

            /// <summary>Required when Scope = GradeLevel.</summary>
            [Display(Name = "Grade Level")]
            public int? GradeLevelId { get; set; }

            [Required(ErrorMessage = "Select a term.")]
            [Display(Name = "Term")]
            public Guid TermId { get; set; }

            [Required(ErrorMessage = "Select a levy type.")]
            [Display(Name = "Levy Type")]
            public int LevyTypeId { get; set; }

            [Required]
            [Range(0.01, 100_000_000, ErrorMessage = "Enter a valid amount greater than zero.")]
            [Display(Name = "Amount")]
            public decimal Amount { get; set; }
        }

        // =============================================================
        //  GET
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
                    Scope = ApplyScope.SingleGrade,
                    GradeId = existing.GradeId,
                    TermId = existing.TermId,
                    LevyTypeId = existing.LevyTypeId,
                    Amount = existing.Amount
                };
            }

            return Page();
        }

        // =============================================================
        //  POST
        // =============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (!_currentUser.HasRight(AccessRights.GenerateInvoices))
                return Forbid();

            await LoadLookupsAsync();

            // Editing always targets a single grade — force it.
            if (IsEditing)
                Input.Scope = ApplyScope.SingleGrade;

            // Scope-specific validation.
            switch (Input.Scope)
            {
                case ApplyScope.SingleGrade:
                    if (!Input.GradeId.HasValue)
                        ModelState.AddModelError("Input.GradeId", "Select a grade.");
                    break;

                case ApplyScope.GradeLevel:
                    if (!Input.GradeLevelId.HasValue)
                        ModelState.AddModelError("Input.GradeLevelId", "Select a grade level.");
                    break;

                case ApplyScope.Everyone:
                    // No extra fields required — the whole Grade table is the target.
                    break;
            }

            if (!ModelState.IsValid)
                return Page();

            return Input.Scope switch
            {
                ApplyScope.SingleGrade => await SaveSingleAsync(),
                ApplyScope.GradeLevel => await SaveByLevelAsync(),
                ApplyScope.Everyone => await SaveEveryoneAsync(),
                _ => Page()
            };
        }

        // =============================================================
        //  SCOPE 1 — Single grade
        // =============================================================
        private async Task<IActionResult> SaveSingleAsync()
        {
            var gradeId = Input.GradeId!.Value;
            var excludeId = Input.Id ?? Guid.Empty;

            var clash = await _context.FeesStructures
                .FirstOrDefaultAsync(f =>
                    f.GradeId == gradeId &&
                    f.TermId == Input.TermId &&
                    f.LevyTypeId == Input.LevyTypeId &&
                    f.CurrencyId == BaseCurrency &&
                    f.Id != excludeId);

            if (clash != null)
            {
                ModelState.AddModelError(string.Empty,
                    "A fee for this grade, term, and levy type already exists.");
                return Page();
            }

            if (Input.Id.HasValue && Input.Id != Guid.Empty)
            {
                var entity = await _context.FeesStructures
                    .FirstOrDefaultAsync(f => f.Id == Input.Id.Value)
                    ?? throw new InvalidOperationException("Fee not found.");

                entity.GradeId = gradeId;
                entity.TermId = Input.TermId;
                entity.LevyTypeId = Input.LevyTypeId;
                entity.Amount = Input.Amount;
                entity.CurrencyId = BaseCurrency;

                TempData["FeesSuccess"] = "Fee updated.";
            }
            else
            {
                _context.FeesStructures.Add(new FeesStructure
                {
                    Id = Guid.NewGuid(),
                    GradeId = gradeId,
                    TermId = Input.TermId,
                    LevyTypeId = Input.LevyTypeId,
                    Amount = Input.Amount,
                    CurrencyId = BaseCurrency,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                });

                TempData["FeesSuccess"] = "Fee added.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./FeesStructures");
        }

        // =============================================================
        //  SCOPE 2 — All grades at a given level (e.g. all Junior)
        // =============================================================
        private async Task<IActionResult> SaveByLevelAsync()
        {
            var levelId = Input.GradeLevelId!.Value;

            var targetGradeIds = await _context.Grades.AsNoTracking()
                .Where(g => g.GradeLevelId == levelId)
                .Select(g => g.Id)
                .ToListAsync();

            if (targetGradeIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty,
                    "No grades are configured under that level.");
                return Page();
            }

            var levelName = ((GradeLevel)levelId).ToDisplayName();

            return await BulkInsertAsync(
                targetGradeIds,
                $"fee for {targetGradeIds.Count} grade(s) in {levelName}");
        }

        // =============================================================
        //  SCOPE 3 — Everyone (all grades)
        // =============================================================
        private async Task<IActionResult> SaveEveryoneAsync()
        {
            var allGradeIds = await _context.Grades.AsNoTracking()
                .Select(g => g.Id)
                .ToListAsync();

            if (allGradeIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty,
                    "No grades are configured yet. Add grades in Configuration first.");
                return Page();
            }

            return await BulkInsertAsync(
                allGradeIds,
                $"fee for all {allGradeIds.Count} grade(s)");
        }

        // =============================================================
        //  Shared bulk insert — skips grades that already have the fee
        // =============================================================
        private async Task<IActionResult> BulkInsertAsync(List<Guid> targetGradeIds, string description)
        {
            var existingGradeIds = await _context.FeesStructures.AsNoTracking()
                .Where(f =>
                    f.TermId == Input.TermId &&
                    f.LevyTypeId == Input.LevyTypeId &&
                    f.CurrencyId == BaseCurrency)
                .Select(f => f.GradeId)
                .ToListAsync();

            var existingSet = existingGradeIds.ToHashSet();
            var gradesToAdd = targetGradeIds.Where(g => !existingSet.Contains(g)).ToList();
            var skipped = targetGradeIds.Count - gradesToAdd.Count;

            if (gradesToAdd.Count == 0)
            {
                ModelState.AddModelError(string.Empty,
                    $"All {targetGradeIds.Count} target grade(s) already have this fee for " +
                    "the selected term and levy type. Edit them individually instead.");
                return Page();
            }

            var now = DateTime.Now;
            var creatorId = _currentUser.UserId ?? Guid.Empty;

            foreach (var gradeId in gradesToAdd)
            {
                _context.FeesStructures.Add(new FeesStructure
                {
                    Id = Guid.NewGuid(),
                    GradeId = gradeId,
                    TermId = Input.TermId,
                    LevyTypeId = Input.LevyTypeId,
                    Amount = Input.Amount,
                    CurrencyId = BaseCurrency,
                    CreatorId = creatorId,
                    CreationDate = now
                });
            }

            await _context.SaveChangesAsync();

            TempData["FeesSuccess"] = skipped > 0
                ? $"Added {description}. {skipped} grade(s) already had this fee and were skipped."
                : $"Added {description}.";

            return RedirectToPage("./FeesStructures");
        }

        // =============================================================
        //  LOOKUPS
        // =============================================================
        private async Task LoadLookupsAsync()
        {
            // ---- Base currency ----
            BaseCurrency = await _context.Currencies.AsNoTracking()
                .Where(c => c.IsBase == true)
                .Select(c => c.Code)
                .FirstOrDefaultAsync() ?? "USD";

            // ---- Grades ----
            Grades = await _context.Grades.AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name })
                .ToListAsync();

            TotalGradeCount = Grades.Count;

            // ---- Grade levels (only those that actually have grades) ----
            var gradeLevelIds = await _context.Grades.AsNoTracking()
                .Select(g => g.GradeLevelId)
                .ToListAsync();

            GradeLevelCounts = gradeLevelIds
                .GroupBy(l => l)
                .ToDictionary(g => g.Key, g => g.Count());

            GradeLevels = Enum.GetValues<GradeLevel>()
                .Where(l => l != GradeLevel.NONE && GradeLevelCounts.ContainsKey((int)l))
                .Select(l =>
                {
                    var count = GradeLevelCounts[(int)l];
                    return new SelectListItem
                    {
                        Value = ((int)l).ToString(),
                        Text = $"{l.ToDisplayName()} ({count} grade{(count == 1 ? "" : "s")})"
                    };
                })
                .ToList();

            // ---- Terms ----
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

            // ---- Levy types ----
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