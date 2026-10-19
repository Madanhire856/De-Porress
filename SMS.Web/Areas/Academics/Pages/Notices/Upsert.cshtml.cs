using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.Notices;
[Authorize]
public class UpsertModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty] public NoticeInput Input { get; set; } = new();
    public bool Published { get; private set; }
    public bool CanPublish => SchoolNotices.CanPublish(access);
    public class NoticeInput
    {
        [Required, StringLength(50)] public string Title { get; set; } = "";
        [Required, StringLength(10000)] public string Body { get; set; } = "";
        [Range(4, 5)] public int Audience { get; set; } = (int)AnnouncementChannel.STAFF_NOTICE;
    }
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!(id.HasValue ? SchoolNotices.CanEdit(access) : SchoolNotices.CanCreate(access))) return Forbid();
        if (id.HasValue)
        {
            var notice = await SchoolNotices.Managed(db).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (notice == null) return NotFound();
            Input = new() { Title = notice.Title, Body = notice.Body ?? "", Audience = notice.ChannelId };
            Published = notice.StatusId == (int)AnnouncementStatus.SENT;
            if (Published && !CanPublish) return Forbid();
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid? id, string intent)
    {
        if (!(id.HasValue ? SchoolNotices.CanEdit(access) : SchoolNotices.CanCreate(access)) || !user.UserId.HasValue) return Forbid();
        Announcement? notice = null;
        if (id.HasValue)
        {
            notice = await SchoolNotices.Managed(db).SingleOrDefaultAsync(x => x.Id == id);
            if (notice == null) return NotFound();
            Published = notice.StatusId == (int)AnnouncementStatus.SENT;
            if (Published && !CanPublish) return Forbid();
        }
        if (intent is not ("draft" or "publish")) return BadRequest();
        if (intent == "publish" && !CanPublish) return Forbid();
        if (Input.Audience != (int)AnnouncementChannel.WEBSITE && Input.Audience != (int)AnnouncementChannel.STAFF_NOTICE)
            ModelState.AddModelError("Input.Audience", "Choose a valid notice audience.");
        if (string.IsNullOrWhiteSpace(Input.Title) || string.IsNullOrWhiteSpace(Input.Body)) ModelState.AddModelError("", "Enter a title and notice text.");
        if (!ModelState.IsValid) return Page();
        if (notice == null)
        {
            notice = new() { Id = Guid.NewGuid(), CreatorId = user.UserId.Value, CreationDate = DateTime.UtcNow, ChannelId = (int)AnnouncementChannel.WEBSITE };
            db.Announcements.Add(notice);
        }
        notice.Title = Input.Title.Trim();
        notice.ChannelId = Input.Audience;
        notice.Body = Input.Body.Trim();
        notice.StatusId = intent == "publish" ? (int)AnnouncementStatus.SENT : (int)AnnouncementStatus.DRAFT;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return new ConflictObjectResult("The notice's publication status changed. Reload it before saving."); }
        TempData["NoticeSaved"] = intent == "publish"
            ? (Input.Audience == (int)AnnouncementChannel.STAFF_NOTICE ? "Notice published for signed-in staff only." : "Notice published on the public school website.")
            : "Draft saved. It is only visible to notice administrators.";
        return RedirectToPage("./Index");
    }
}
