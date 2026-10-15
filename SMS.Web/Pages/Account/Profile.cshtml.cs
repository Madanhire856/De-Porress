using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;

namespace SMS.Web.Pages.Account;

[Authorize]
public class ProfileModel(SMSDbContext context, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public string Email { get; private set; } = "";
    public string Role { get; private set; } = "";
    public string GroupName { get; private set; } = "Not assigned";
    public DateTime Joined { get; private set; }
    public bool IsEditing { get; private set; }
    public string DisplayName { get; private set; } = "";
    public string LoginId { get; private set; } = "";
    public string UserNumber { get; private set; } = "";
    public string? Mobile { get; private set; }
    public DateTime? LastLogin { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(bool edit = false)
    {
        IsEditing = edit;
        var account = await LoadAccountAsync();
        if (account == null) return Forbid();
        Input = new ProfileInput { Name = account.Name ?? "", Mobile = account.Mobile };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IsEditing = true;
        var account = await LoadAccountAsync();
        if (account == null) return Forbid();
        if (!ModelState.IsValid) return Page();

        // Only personal contact fields can be changed here. Identity and permissions
        // always come from the signed-in account, never from posted IDs or roles.
        account.Name = Input.Name.Trim();
        account.Mobile = string.IsNullOrWhiteSpace(Input.Mobile) ? null : Input.Mobile.Trim();
        await context.SaveChangesAsync();
        StatusMessage = "Your profile has been updated.";
        return RedirectToPage();
    }

    private async Task<SMS.Data.User?> LoadAccountAsync()
    {
        if (!currentUser.IsAuthenticated) return null;
        var id = currentUser.UserId;
        if (!id.HasValue || id == Guid.Empty) return null;

        var account = await context.Users.Include(u => u.Group)
            .SingleOrDefaultAsync(u => u.Id == id.Value && u.IsActive);
        if (account == null) return null;
        Email = account.Email;
        Role = account.RoleId == 0 ? "Not assigned" : account.GetRoleDisplayName();
        GroupName = account.Group?.Name ?? "Not assigned";
        Joined = account.CreationDate;
        DisplayName = string.IsNullOrWhiteSpace(account.Name) ? account.Email : account.Name;
        LoginId = account.LoginId;
        Mobile = account.Mobile;
        LastLogin = account.LastLoginDate;
        context.ApplyUserNumber(account);
        UserNumber = account.UserNumber;
        return account;
    }

    public sealed class ProfileInput
    {
        [Required, StringLength(50), Display(Name = "Display name")]
        public string Name { get; set; } = "";

        [StringLength(15), Phone, Display(Name = "Contact number")]
        public string? Mobile { get; set; }
    }
}
