using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.Notices;

[Authorize]
public class IndexModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Audience { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<Announcement> Notices { get; private set; } = [];
    public int Total { get; private set; }
    public bool HasNext => PageNumber * 15 < Total;
    public async Task<IActionResult> OnGetAsync()
    {
        if (!SchoolNotices.CanManage(access)) return Forbid();
        var query = SchoolNotices.Managed(db).AsNoTracking();
        if (Audience == "public") query = query.Where(x => x.ChannelId == (int)AnnouncementChannel.WEBSITE);
        if (Audience == "staff") query = query.Where(x => x.ChannelId == (int)AnnouncementChannel.STAFF_NOTICE);
        if (!string.IsNullOrWhiteSpace(SearchTerm)) query = query.Where(x => x.Title.Contains(SearchTerm) || (x.Body != null && x.Body.Contains(SearchTerm)));
        if (Status == "published") query = query.Where(x => x.StatusId == (int)AnnouncementStatus.SENT);
        if (Status == "draft") query = query.Where(x => x.StatusId == (int)AnnouncementStatus.DRAFT);
        Total = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (Total + 14) / 15));
        Notices = await query.OrderByDescending(x => x.CreationDate).ThenByDescending(x => x.Id).Skip((PageNumber - 1) * 15).Take(15).ToListAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostWithdrawAsync(Guid id)
    {
        if (!SchoolNotices.CanManage(access)) return Forbid();
        var notice = await SchoolNotices.Managed(db).SingleOrDefaultAsync(x => x.Id == id);
        if (notice == null) return NotFound();
        notice.StatusId = (int)AnnouncementStatus.DRAFT;
        await db.SaveChangesAsync();
        TempData["NoticeSaved"] = "Notice withdrawn and kept as a draft.";
        return RedirectToPage();
    }
}
