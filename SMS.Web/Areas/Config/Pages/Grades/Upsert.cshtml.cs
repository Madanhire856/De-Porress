using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Grades.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Grades
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
        public GradeVM grade { get; set; } = new();

        public List<SelectListItem> Levels { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // ---- Rights guard ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateGrades))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditGrades))
                return Forbid();

            LoadLookups();

            if (id != null)
            {
                // === EDIT ===
                var entity = await _context.Grades
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (entity != null)
                {
                    grade = new GradeVM
                    {
                        Id = entity.Id,
                        Name = entity.Name,
                        GradeLevelId = entity.GradeLevelId
                    };
                }
            }
            else
            {
                // === CREATE ===
                grade = new GradeVM
                {
                    Id = Guid.NewGuid()
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            // ---- Same guard on POST ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateGrades))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditGrades))
                return Forbid();

            LoadLookups();

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            var clashName = await _context.Grades
                .FirstOrDefaultAsync(g => g.Name == grade.Name && g.Id != excludeId);
            if (clashName != null)
            {
                ModelState.AddModelError("grade.Name", "A grade with this name already exists.");
                return Page();
            }

            Grade entity;

            if (id == null)
            {
                entity = new Grade
                {
                    Id = grade.Id == Guid.Empty ? Guid.NewGuid() : grade.Id,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };
                await _context.Grades.AddAsync(entity);
            }
            else
            {
                entity = await _context.Grades
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();
            }

            entity.Name = grade.Name;
            entity.GradeLevelId = grade.GradeLevelId ?? 0;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }

        private void LoadLookups()
        {
            Levels = EnumExtensions.AllGradeLevels()
                .Select(l => new SelectListItem
                {
                    Value = ((int)l).ToString(),
                    Text = l.ToDisplayName()
                })
                .ToList();
        }
    }
}