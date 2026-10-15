using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Terms.ViewModels;
using System;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Terms
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
        public TermVM term { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null && !_currentUser.HasRight(AccessRights.CreateTerms))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditTerms))
                return Forbid();

            if (id != null)
            {
                var entity = await _context.Terms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (entity != null)
                {
                    term = new TermVM
                    {
                        Id = entity.Id,
                        Name = entity.Name ?? "",
                        AcademicYear = entity.AcademicYear,
                        TermNumber = entity.Number,
                        StartDate = entity.StartDate,
                        EndDate = entity.EndDate
                    };
                }
            }
            else
            {
                term = new TermVM
                {
                    Id = Guid.NewGuid(),
                    Name = "",
                    AcademicYear = DateTime.Today.Year,
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null && !_currentUser.HasRight(AccessRights.CreateTerms))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditTerms))
                return Forbid();

            if (term.StartDate.HasValue && term.EndDate.HasValue
                && term.EndDate.Value <= term.StartDate.Value)
            {
                ModelState.AddModelError("term.EndDate", "End date must be after the start date.");
            }

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            // Uniqueness: same year + same term number
            var clash = await _context.Terms
                .FirstOrDefaultAsync(t =>
                    t.AcademicYear == term.AcademicYear &&
                    t.Number == term.TermNumber &&
                    t.Id != excludeId);

            if (clash != null)
            {
                ModelState.AddModelError("term.TermNumber",
                    $"Term {term.TermNumber} of {term.AcademicYear} already exists.");
                return Page();
            }

            // Optional: warn if the name is already used in the same year
            var nameClash = await _context.Terms
                .FirstOrDefaultAsync(t =>
                    t.AcademicYear == term.AcademicYear &&
                    t.Name == term.Name &&
                    t.Id != excludeId);

            if (nameClash != null)
            {
                ModelState.AddModelError("term.Name",
                    $"A term named \"{term.Name}\" already exists in {term.AcademicYear}.");
                return Page();
            }

            Term entity;

            if (id == null)
            {
                entity = new Term
                {
                    Id = term.Id == Guid.Empty ? Guid.NewGuid() : term.Id,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };
                await _context.Terms.AddAsync(entity);
            }
            else
            {
                entity = await _context.Terms
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (entity == null)
                    return NotFound();
            }

            // Map form → entity
            entity.Name = term.Name.Trim();
            entity.AcademicYear = term.AcademicYear;
            entity.Number = term.TermNumber;
            entity.StartDate = term.StartDate ?? DateTime.Today;
            entity.EndDate = term.EndDate ?? DateTime.Today;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }
    }
}