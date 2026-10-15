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

namespace SMS.Web.Areas.Sports.Pages.Sporting
{
    [Authorize]
    public class AddCompetitionModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AddCompetitionModel(SMSDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // The sport this competition is being created for
        public Sport Sport { get; set; } = null!;

        [BindProperty]
        public CompetitionInput Input { get; set; } = new();

        public List<TermOption> TermOptions { get; set; } = new();
        public List<SelectListItem> LevelOptions { get; set; } = new();
        public List<string> TierSuggestions { get; set; } = new();

        // ==============================================================
        //  INPUT MODEL
        // ==============================================================
        public class CompetitionInput
        {
            public Guid? Id { get; set; }

            [Required(ErrorMessage = "Name is required.")]
            [StringLength(150)]
            [Display(Name = "Competition Name")]
            public string Name { get; set; } = "";

            [Required(ErrorMessage = "Tier is required.")]
            [StringLength(50)]
            [Display(Name = "Tier / Age Group")]
            public string Tier { get; set; } = "";

            [Required(ErrorMessage = "Level is required.")]
            [Display(Name = "Level")]
            public int LevelId { get; set; }

            [Required(ErrorMessage = "Academic year is required.")]
            [Range(2000, 2100, ErrorMessage = "Enter a valid year.")]
            [Display(Name = "Academic Year")]
            public int AcademicYear { get; set; }

            [Required(ErrorMessage = "Term is required.")]
            [Display(Name = "Term")]
            public Guid TermId { get; set; }
        }

        // ==============================================================
        //  TERM OPTION (label + year for auto-fill)
        // ==============================================================
        public class TermOption
        {
            public Guid Id { get; set; }
            public string Label { get; set; } = "";
            public int AcademicYear { get; set; }
        }

        // ==============================================================
        //  GET
        // ==============================================================
        public async Task<IActionResult> OnGetAsync(Guid sportId)
        {
            if (!_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            Sport = await _context.Sports
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sportId);

            if (Sport == null) return NotFound();

            await LoadOptionsAsync();

            // Defaults for a new competition
            Input.AcademicYear = DateTime.Now.Year;
            Input.LevelId = (int)CompetitionLevel.INTER_HOUSE;

            return Page();
        }

        // ==============================================================
        //  POST
        // ==============================================================
        public async Task<IActionResult> OnPostAsync(Guid sportId)
        {
            if (!_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            Sport = await _context.Sports
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sportId);

            if (Sport == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                return Page();
            }

            // ---- Verify the selected term exists and matches the academic year ----
            var term = await _context.Terms
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == Input.TermId);

            if (term == null)
            {
                ModelState.AddModelError("Input.TermId", "Selected term not found.");
            }
            else if (term.AcademicYear != Input.AcademicYear)
            {
                ModelState.AddModelError("Input.AcademicYear",
                    $"Academic year must match the selected term ({term.AcademicYear}).");
            }

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                return Page();
            }

            // ---- Persist ----
            _context.Competitions.Add(new Competition
            {
                Id = Guid.NewGuid(),
                Name = Input.Name.Trim(),
                SportId = sportId,
                Tier = Input.Tier.Trim(),
                LevelId = Input.LevelId,
                AcademicYear = Input.AcademicYear,
                TermId = Input.TermId,
                CreationDate = DateTime.Now,
                CreatorId = (Guid)_currentUser.UserId
            });

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        // ==============================================================
        //  LOAD OPTIONS
        // ==============================================================
        private async Task LoadOptionsAsync()
        {
            // ---- Terms — most recent year first, then by number ----
            var termRows = await _context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.AcademicYear)
                .ThenBy(t => t.Number)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.AcademicYear,
                    t.Number
                })
                .ToListAsync();

            TermOptions = termRows
                .Select(t => new TermOption
                {
                    Id = t.Id,
                    AcademicYear = t.AcademicYear,
                    Label = string.IsNullOrWhiteSpace(t.Name)
                        ? $"{t.AcademicYear} · Term {t.Number}"
                        : $"{t.Name} ({t.AcademicYear} · Term {t.Number})"
                })
                .ToList();

            // ---- Levels ----
            LevelOptions = EnumExtensions.AllCompetitionLevels()
                .Select(l => new SelectListItem
                {
                    Value = ((int)l).ToString(),
                    Text = l.ToDisplayName()
                })
                .ToList();

            // ---- Tier suggestions (from existing competitions) ----
            TierSuggestions = await _context.Competitions
                .AsNoTracking()
                .Where(c => c.Tier != null)
                .Select(c => c.Tier!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }
    }
}