using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Houses.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Houses
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
        public HouseVM house { get; set; } = new();

        public List<SelectListItem> Masters { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // ---- Rights guard ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateHouses))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            if (id != null)
            {
                var entity = await _context.Houses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (entity != null)
                {
                    house = new HouseVM
                    {
                        Id = entity.Id,
                        Name = entity.Name,
                        Color = entity.Color,
                        MasterId = entity.MasterId
                    };

                    await LoadLookupsAsync(includeStaffId: entity.MasterId);
                    return Page();
                }
            }

            house = new HouseVM
            {
                Id = Guid.NewGuid()
            };
            await LoadLookupsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            // ---- Same guard on POST ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateHouses))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditHouses))
                return Forbid();

            await LoadLookupsAsync(includeStaffId: house.MasterId);

            if (!ModelState.IsValid)
                return Page();

            Guid excludeId = id ?? Guid.Empty;

            var clashName = await _context.Houses
                .FirstOrDefaultAsync(h => h.Name == house.Name && h.Id != excludeId);
            if (clashName != null)
            {
                ModelState.AddModelError("house.Name", "A house with this name already exists.");
                return Page();
            }

            House entity;

            if (id == null)
            {
                entity = new House
                {
                    Id = house.Id == Guid.Empty ? Guid.NewGuid() : house.Id,
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now
                };
                await _context.Houses.AddAsync(entity);
            }
            else
            {
                entity = await _context.Houses
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();
            }

            entity.Name = house.Name;
            entity.Color = house.Color;
            entity.MasterId = house.MasterId ?? Guid.Empty;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }

        private async Task LoadLookupsAsync(Guid? includeStaffId = null)
        {
            var eligibleCategoryIds = new[]
            {
                (int)StaffCategory.GENERAL_TEACHER,
                (int)StaffCategory.SENIOR_TEACHER,
            };

            var rows = await _context.Staff
                .AsNoTracking()
                .Where(s =>
                    s.IsActive == true
                    && (eligibleCategoryIds.Contains(s.CategoryId)
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

            Masters = rows
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Name} {s.Surname} — {((StaffCategory)s.CategoryId).ToDisplayName()}"
                })
                .ToList();
        }
    }
}