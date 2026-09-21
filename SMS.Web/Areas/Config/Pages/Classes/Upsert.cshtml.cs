using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Classes.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Classes
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
        public ClassVM cls { get; set; } = new();

        public List<SelectListItem> Grades { get; set; } = new();
        public List<SelectListItem> Teachers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // ---- Rights guard ----
            if (id == null)
            {
                // Create mode
                if (!_currentUser.HasRight(AccessRights.CreateClasses))
                    return Forbid();
            }
            else
            {
                // Edit mode
                if (!_currentUser.HasRight(AccessRights.EditClasses))
                    return Forbid();
            }

            if (id != null)
            {
                var entity = await _context.Classes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (entity != null)
                {
                    cls = new ClassVM
                    {
                        Id = entity.Id,
                        GradeId = entity.GradeId,
                        Name = entity.Name,
                        Capacity = entity.Capacity,
                        ClassTeacherId = entity.ClassTeacherId
                    };

                    await LoadLookupsAsync(includeStaffId: entity.ClassTeacherId);
                    return Page();
                }
            }

            cls = new ClassVM { Id = Guid.NewGuid() };
            await LoadLookupsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            // ---- Same guard on POST (defense in depth) ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateClasses))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditClasses))
                return Forbid();

            await LoadLookupsAsync(includeStaffId: cls.ClassTeacherId);

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            var clashName = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == cls.Name && c.Id != excludeId);
            if (clashName != null)
            {
                ModelState.AddModelError("cls.Name", "A class with this name already exists.");
                return Page();
            }

            Data.Class entity;

            if (id == null)
            {
                entity = new Data.Class
                {
                    Id = cls.Id == Guid.Empty ? Guid.NewGuid() : cls.Id,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };
                await _context.Classes.AddAsync(entity);
            }
            else
            {
                entity = await _context.Classes
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();
            }

            entity.GradeId = cls.GradeId ?? Guid.Empty;
            entity.Name = cls.Name;
            entity.Capacity = cls.Capacity;
            entity.ClassTeacherId = cls.ClassTeacherId ?? Guid.Empty;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }

        private async Task LoadLookupsAsync(Guid? includeStaffId = null)
        {
            Grades = await _context.Grades
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.Name
                })
                .ToListAsync();

            var teachingCategoryIds = new[]
            {
                (int)StaffCategory.GENERAL_TEACHER,
                (int)StaffCategory.SENIOR_TEACHER,
                (int)StaffCategory.STUDENT_TEACHER
            };

            var teacherRows = await _context.Staff
                .AsNoTracking()
                .Where(s =>
                    s.IsActive == true
                    && (teachingCategoryIds.Contains(s.CategoryId)
                        || (includeStaffId.HasValue && s.Id == includeStaffId.Value)))
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Surname,
                    s.CategoryId
                })
                .ToListAsync();

            Teachers = teacherRows
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Name} {s.Surname} — {((StaffCategory)s.CategoryId).ToDisplayName()}"
                })
                .ToList();
        }
    }
}