using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Villages.ViewModels;
using System;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Villages
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
        public VillageVM village { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // ---- Rights guard ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateVillages))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditVillages))
                return Forbid();

            if (id != null)
            {
                // === EDIT ===
                var entity = await _context.Villages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (entity != null)
                {
                    village = new VillageVM
                    {
                        Id = entity.Id,
                        Name = entity.Name,
                        Headman = entity.Headman,
                        Chief = entity.Chief
                    };
                }
            }
            else
            {
                // === CREATE ===
                village = new VillageVM
                {
                    Id = Guid.NewGuid()
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            // ---- Same guard on POST ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateVillages))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditVillages))
                return Forbid();

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            var clashName = await _context.Villages
                .FirstOrDefaultAsync(v => v.Name == village.Name && v.Id != excludeId);
            if (clashName != null)
            {
                ModelState.AddModelError("village.Name", "A village with this name already exists.");
                return Page();
            }

            Village entity;

            if (id == null)
            {
                // === CREATE ===
                entity = new Village
                {
                    Id = village.Id == Guid.Empty ? Guid.NewGuid() : village.Id,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };
                await _context.Villages.AddAsync(entity);
            }
            else
            {
                // === EDIT ===
                entity = await _context.Villages
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();
            }

            // Map form → entity
            entity.Name = village.Name;
            entity.Headman = village.Headman;
            entity.Chief = village.Chief;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }
    }
}