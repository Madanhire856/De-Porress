using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Sports.ViewModels;
using System;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Sports
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
        public SportVM sport { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // ---- Rights guard ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateSports))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditSports))
                return Forbid();

            if (id != null)
            {
                var entity = await _context.Sports
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (entity != null)
                {
                    sport = new SportVM
                    {
                        Id = entity.Id,
                        Name = entity.Name
                    };

                    return Page();
                }
            }

            sport = new SportVM
            {
                Id = Guid.NewGuid()
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            // ---- Same guard on POST ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateSports))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditSports))
                return Forbid();

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            var clashName = await _context.Sports
                .FirstOrDefaultAsync(s => s.Name == sport.Name && s.Id != excludeId);
            if (clashName != null)
            {
                ModelState.AddModelError("sport.Name", "A sport with this name already exists.");
                return Page();
            }

            Sport entity;

            if (id == null)
            {
                entity = new Sport
                {
                    Id = sport.Id == Guid.Empty ? Guid.NewGuid() : sport.Id,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };
                await _context.Sports.AddAsync(entity);
            }
            else
            {
                entity = await _context.Sports
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();
            }

            entity.Name = sport.Name;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }
    }
}