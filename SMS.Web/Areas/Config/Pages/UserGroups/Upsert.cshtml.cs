using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SMS.Data;
using SMS.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.UserGroups
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
        public UserGroup UserGroupVM { get; set; } = new();

        [BindProperty]
        public List<long> SelectedRights { get; set; } = new();

        public List<RightCategory> RightsCategories { get; set; } = new();

        public bool IsEditMode { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // ---- Rights guard ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateUserGroups))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditUserGroups))
                return Forbid();

            RightsCategories = RightsCatalog.GetCategories();
            if (id == null || id == Guid.Empty)
            {
                IsEditMode = false;
                UserGroupVM = new UserGroup
                {
                    Id = Guid.NewGuid(),
                    CreationDate = DateTime.UtcNow,
                    CreatorId = _currentUser.UserId ?? Guid.Empty
                };
                return Page();
            }

            // --- EDIT MODE ---
            IsEditMode = true;
            var existing = await _context.UserGroups
                .FirstOrDefaultAsync(g => g.Id == id);

            if (existing == null)
                return NotFound();

            UserGroupVM = existing;

            if (existing.RightsId.HasValue)
            {
                long rightsValue = existing.RightsId.Value;
                foreach (AccessRights right in Enum.GetValues(typeof(AccessRights)))
                {
                    if (right == AccessRights.None) continue;
                    if ((rightsValue & (long)right) == (long)right)
                    {
                        SelectedRights.Add((long)right);
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            // ---- Same guard on POST ----
            bool isEdit = id != Guid.Empty;
            if (!isEdit && !_currentUser.HasRight(AccessRights.CreateUserGroups))
                return Forbid();
            if (isEdit && !_currentUser.HasRight(AccessRights.EditUserGroups))
                return Forbid();

            ModelState.Remove("UserGroupVM.Creator");
            RightsCategories = RightsCatalog.GetCategories();

            if (!ModelState.IsValid)
            {
                IsEditMode = await _context.UserGroups.AnyAsync(g => g.Id == UserGroupVM.Id);
                return Page();
            }

            long combinedRights = 0;
            if (SelectedRights != null)
                foreach (var value in SelectedRights) combinedRights |= value;

            UserGroupVM.RightsId = combinedRights == 0 ? null : combinedRights;

            var existing = await _context.UserGroups
                .FirstOrDefaultAsync(g => g.Id == UserGroupVM.Id);

            if (existing == null)
            {
                // INSERT
                UserGroupVM.CreationDate = DateTime.UtcNow;
                UserGroupVM.CreatorId = _currentUser.UserId ?? Guid.Empty;
                _context.UserGroups.Add(UserGroupVM);
            }
            else
            {
                existing.Name = UserGroupVM.Name;
                existing.Description = UserGroupVM.Description;
                existing.RightsId = UserGroupVM.RightsId;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = UserGroupVM.Id });
        }
    }
}