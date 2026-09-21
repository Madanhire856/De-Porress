using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Config.Pages.Users.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Web.Areas.Config.Pages.Users
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
        public UserVM user { get; set; } = new(); 

        public List<UserGroup> UserGroups { get; set; } = new();

        public async Task OnGetAsync(Guid? id)
        {
            UserGroups = await _context.UserGroups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (id != null)
            {
                var entity = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity != null)
                {
                    user = new UserVM
                    {
                        Id = entity.Id,
                        Name = entity.Name,
                        Email = entity.Email,
                        LoginId = entity.LoginId,
                        Mobile = entity.Mobile,
                        RoleId = entity.RoleId,
                        GroupId = entity.GroupId,
                        IsActive = entity.IsActive,
                    };
                }
            }
            else
            {
                user = new UserVM
                {
                    Id = Guid.NewGuid(),
                    IsActive = true
                };
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            UserGroups = await _context.UserGroups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (!ModelState.IsValid)
                return Page();

            // Exclude the record being edited (or nothing, when creating)
            Guid excludeId = id ?? Guid.Empty;

            // Uniqueness — LoginId
            var clashLogin = await _context.Users
                .FirstOrDefaultAsync(x => x.LoginId == user.LoginId && x.Id != excludeId);

            if (clashLogin != null)
            {
                ModelState.AddModelError("user.LoginId", "A user already exists with this Login ID.");
                return Page();
            }

            // Uniqueness — Email
            var clashEmail = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == user.Email && x.Id != excludeId);

            if (clashEmail != null)
            {
                ModelState.AddModelError("user.Email", "A user already exists with this email.");
                return Page();
            }

            User entity;

            if (id == null)
            {
                // === CREATE ===
                entity = new User
                {
                    Id = Guid.NewGuid(),
                    CreatorId = _currentUser.UserId ?? Guid.Empty,
                    CreationDate = DateTime.Now,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    IsActive = user.IsActive,
                    IsEmailConfirmed = false,
                    TwoFactorAuthEnabled = false
                };

                await _context.Users.AddAsync(entity);
            }
            else
            {
                // === EDIT ===
                entity = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return NotFound();

                entity.IsActive = user.IsActive;
            }

            entity.Name = user.Name;
            entity.Email = user.Email;
            entity.LoginId = user.LoginId;
            entity.Mobile = user.Mobile;
            entity.RoleId = user.RoleId;
            entity.GroupId = user.GroupId;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }
    }
}