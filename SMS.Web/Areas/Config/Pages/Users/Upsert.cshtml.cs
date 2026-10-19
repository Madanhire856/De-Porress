using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
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
        private readonly ICognitoUserService _cognito;
        private readonly ILogger<UpsertModel>? _logger;

        public UpsertModel(SMSDbContext context, ICurrentUserService currentUser, ICognitoUserService cognito,
            ILogger<UpsertModel>? logger = null)
        {
            _context = context;
            _currentUser = currentUser;
            _cognito = cognito;
            _logger = logger;
        }

        [BindProperty]
        public UserVM user { get; set; } = new();
        [BindProperty] public bool SendInvitation { get; set; } = true;
        public bool InvitationsEnabled => _cognito.InvitationsEnabled;
        public bool IsEdit { get; private set; }

        public List<UserGroup> UserGroups { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            IsEdit = id.HasValue;
            // ---- Rights guard ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateUsers))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditUsers))
                return Forbid();

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
                    if (RoleAssignmentPolicy.IsSystemAdministrator(entity.RoleId) && !RoleAssignmentPolicy.IsSystemAdministrator(_currentUser.RoleId)) return Forbid();
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            IsEdit = id.HasValue;
            // ---- Same guard on POST ----
            if (id == null && !_currentUser.HasRight(AccessRights.CreateUsers))
                return Forbid();
            if (id != null && !_currentUser.HasRight(AccessRights.EditUsers))
                return Forbid();
            if (!RoleAssignmentPolicy.CanAssign(_currentUser.RoleId, user.RoleId)) return Forbid();
            if (id.HasValue && !RoleAssignmentPolicy.IsSystemAdministrator(_currentUser.RoleId)
                && await _context.Users.AnyAsync(x => x.Id == id && (x.RoleId & (int)UserRole.ADMIN) != 0)) return Forbid();

            UserGroups = await _context.UserGroups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (!RoleAssignmentPolicy.IsDefined(user.RoleId) || (user.IsActive && user.RoleId == 0))
                ModelState.AddModelError("user.RoleId", "Choose a valid role for an active account.");
            user.Email = user.Email?.Trim() ?? "";
            user.LoginId = user.LoginId?.Trim() ?? "";
            user.Name = user.Name?.Trim();
            if (!id.HasValue && InvitationsEnabled && SendInvitation && !user.IsActive)
                ModelState.AddModelError(nameof(SendInvitation), "Activate the account before sending an invitation.");
            if (user.GroupId == Guid.Empty) user.GroupId = null;
            if (user.GroupId.HasValue && !UserGroups.Any(x => x.Id == user.GroupId))
                ModelState.AddModelError("user.GroupId", "Choose an existing user group.");

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
                .FirstOrDefaultAsync(x => x.Email.ToLower() == user.Email.ToLower() && x.Id != excludeId);

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
            if (entity.IsActive && !entity.ActivationDate.HasValue) entity.ActivationDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogError(ex, "Failed to save user. Request ID: {RequestId}", HttpContext.TraceIdentifier);
                ModelState.AddModelError(string.Empty,
                    "The user could not be saved. Check the values and try again. If this continues, contact the administrator with request ID: "
                    + HttpContext.TraceIdentifier);
                return Page();
            }

            if (!id.HasValue && InvitationsEnabled && SendInvitation)
            {
                try
                {
                    var result = await _cognito.InviteAsync(entity.Email, entity.Name, HttpContext.RequestAborted);
                    TempData["InvitationStatus"] = result == InvitationResult.Sent
                        ? "User created and invitation sent. They will set a password and verify their email when they sign in."
                        : "User created. This email already has a sign-in account and can use its existing login.";
                }
                catch (CognitoUserOperationException ex)
                {
                    TempData["InvitationError"] = "User created, but no invitation was confirmed. " + ex.Message;
                }
            }

            return RedirectToPage("./Details", new { area = "Config", id = entity.Id });
        }
    }
}
