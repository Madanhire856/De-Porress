using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Subjects.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Subjects
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
        public SubjectVM subject { get; set; } = new();

        public List<SelectListItem> Categories { get; set; } = new();

        public async Task OnGetAsync(Guid? id)
        {
            LoadLookups();

            if (id != null)
            {
                // === EDIT ===
                var entity = await _context.Subjects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity != null)
                {
                    subject = new SubjectVM
                    {
                        Id = entity.Id,
                        Name = entity.Name,
                        Code = entity.Code,
                        CategoryId = entity.CategoryId
                    };
                }
            }
            else
            {
                // === CREATE ===
                subject = new SubjectVM
                {
                    Id = Guid.NewGuid()
                };
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            LoadLookups();

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            // Uniqueness — Code
            var clashCode = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code == subject.Code && s.Id != excludeId);
            if (clashCode != null)
            {
                ModelState.AddModelError("subject.Code", "This subject code is already in use.");
                return Page();
            }

            // Uniqueness — Name
            var clashName = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Name == subject.Name && s.Id != excludeId);
            if (clashName != null)
            {
                ModelState.AddModelError("subject.Name", "A subject with this name already exists.");
                return Page();
            }

            Subject entity;

            if (id == null)
            {
                // === CREATE ===
                entity = new Subject
                {
                    Id = subject.Id == Guid.Empty ? Guid.NewGuid() : subject.Id,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };
                await _context.Subjects.AddAsync(entity);
            }
            else
            {
                // === EDIT ===
                entity = await _context.Subjects
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();
            }

            // Map form → entity
            entity.Name = subject.Name;
            entity.Code = subject.Code;
            entity.CategoryId = subject.CategoryId ?? 0;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }

        private void LoadLookups()
        {
            Categories = EnumExtensions.AllSubjectCategories()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToDisplayName()
                })
                .ToList();
        }
    }
}