using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Services;
using System;
using System.Linq;

namespace SMS.Web.Areas.Config.Pages.Users
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly SMSDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ICognitoUserService _cognito;

        public DetailsModel(SMSDbContext context, ICurrentUserService currentUser, ICognitoUserService cognito)
        {
            _context = context;
            _currentUser = currentUser;
            _cognito = cognito;
        }

        public User UserVM { get; set; } = null!;

        public string? CreatorEmail { get; set; }
        public string? GroupName { get; set; }
        public bool CanInvite => _cognito.InvitationsEnabled
            && UserVM.IsActive && _currentUser.HasRight(AccessRights.CreateUsers)
            && _currentUser.HasRight(AccessRights.EditUsers)
            && RoleAssignmentPolicy.CanAssign(_currentUser.RoleId, UserVM.RoleId);

        public async Task<IActionResult> OnPostInviteAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!_currentUser.HasRight(AccessRights.CreateUsers) || !_currentUser.HasRight(AccessRights.EditUsers))
                return Forbid();
            var target = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (target == null) return NotFound();
            if (!RoleAssignmentPolicy.CanAssign(_currentUser.RoleId, target.RoleId)) return Forbid();
            if (!target.IsActive)
            {
                TempData["InvitationError"] = "Activate this account before sending an invitation.";
                return RedirectToPage(new { id });
            }
            try
            {
                var result = await _cognito.InviteAsync(target.Email, target.Name, cancellationToken);
                TempData["InvitationStatus"] = result == InvitationResult.Sent
                    ? "Invitation sent. Any previous temporary password has been replaced."
                    : "This email already has a sign-in account. They can sign in or use Forgot password.";
            }
            catch (CognitoUserOperationException ex) { TempData["InvitationError"] = ex.Message; }
            return RedirectToPage(new { id });
        }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return RedirectToPage("./Index");

            UserVM = _context.Users
                .FirstOrDefault(u => u.Id == id);

            if (UserVM == null)
                return NotFound();
            _context.ApplyUserNumber(UserVM);
            if (UserVM.CreatorId.HasValue)
            {
                CreatorEmail = _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == UserVM.CreatorId.Value)
                    .Select(u => u.Email)
                    .FirstOrDefault();
            }

            // Resolve group name if assigned
            if (UserVM.GroupId.HasValue)
            {
                GroupName = _context.UserGroups
                    .AsNoTracking()
                    .Where(g => g.Id == UserVM.GroupId.Value)
                    .Select(g => g.Name)
                    .FirstOrDefault();
            }

            return Page();
        }
    }
}
