using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
namespace SMS.Web.Areas.Academics.Pages.Sdc;
[Authorize]
public class MemberModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty] public MemberInput Input { get; set; } = new();
    public List<SelectListItem> Terms { get; private set; } = [];
    public List<SelectListItem> Positions { get; private set; } = [];
    public bool IsEdit { get; private set; }
    public class MemberInput
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required] public Guid? TermId { get; set; }
        [Required] public Guid? PositionId { get; set; }
        [StringLength(20), Phone] public string? Phone { get; set; }
        [StringLength(100), EmailAddress] public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!SdcAccess.CanManage(access)) return Forbid();
        IsEdit = id.HasValue;
        if (id.HasValue)
        {
            var member = await db.SdcMembers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (member == null) return NotFound();
            Input = new() { Name = member.Name, TermId = member.TermId, PositionId = member.PositionId, Phone = member.Phone, Email = member.Email, IsActive = member.IsActive };
        }
        await LookupsAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (!SdcAccess.CanManage(access) || !user.UserId.HasValue) return Forbid();
        IsEdit = id.HasValue;
        var member = id.HasValue ? await db.SdcMembers.SingleOrDefaultAsync(x => x.Id == id) : null;
        if (id.HasValue && member == null) return NotFound();
        if (member != null && member.TermId != Input.TermId)
            ModelState.AddModelError("Input.TermId", "To appoint this member for another term, add a new membership record. Keep this record for history.");
        var originalTermId = member?.TermId;
        var originalPositionId = member?.PositionId;
        if (!await db.SdcTerms.AnyAsync(x => x.Id == Input.TermId && (x.IsActive || x.Id == originalTermId)))
            ModelState.AddModelError("Input.TermId", "Choose an active committee term.");
        if (!await db.SdcPositions.AnyAsync(x => x.Id == Input.PositionId && (x.IsActive || x.Id == originalPositionId)))
            ModelState.AddModelError("Input.PositionId", "Choose an active position.");
        if (string.IsNullOrWhiteSpace(Input.Name)) ModelState.AddModelError("Input.Name", "Enter the member's name.");
        await LookupsAsync();
        if (!ModelState.IsValid) return Page();
        if (member == null)
        {
            member = new() { Id = Guid.NewGuid(), CreatorId = user.UserId.Value, CreationDate = DateTime.UtcNow };
            db.SdcMembers.Add(member);
        }
        member.Name = Input.Name.Trim(); member.TermId = Input.TermId!.Value; member.PositionId = Input.PositionId!.Value;
        member.Phone = string.IsNullOrWhiteSpace(Input.Phone) ? null : Input.Phone.Trim();
        member.Email = string.IsNullOrWhiteSpace(Input.Email) ? null : Input.Email.Trim();
        member.IsActive = Input.IsActive;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "The membership could not be saved. Refresh and check the selected term and position."); return Page(); }
        TempData["SdcSaved"] = "SDC membership saved.";
        return RedirectToPage("./Index", new { TermId = member.TermId });
    }
    private async Task LookupsAsync()
    {
        Terms = await db.SdcTerms.AsNoTracking().Where(x => x.IsActive || (IsEdit && x.Id == Input.TermId))
            .OrderByDescending(x => x.StartDate).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync();
        Positions = await db.SdcPositions.AsNoTracking().Where(x => x.IsActive || (IsEdit && x.Id == Input.PositionId))
            .OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync();
    }
}
