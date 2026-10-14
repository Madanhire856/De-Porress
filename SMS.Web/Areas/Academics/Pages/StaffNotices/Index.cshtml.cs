using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.StaffNotices;
[Authorize]
public class IndexModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<Announcement> Notices { get; private set; } = [];
    public bool HasNext { get; private set; }
    public async Task<IActionResult> OnGetAsync()
    {
        if (!SchoolNotices.CanReadStaff(access)) return Forbid();
        var query = SchoolNotices.StaffPublished(db);
        if (!string.IsNullOrWhiteSpace(SearchTerm)) query = query.Where(x => x.Title.Contains(SearchTerm) || (x.Body != null && x.Body.Contains(SearchTerm)));
        var count = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (count + 11) / 12));
        Notices = await query.OrderByDescending(x => x.CreationDate).ThenByDescending(x => x.Id).Skip((PageNumber - 1) * 12).Take(12).ToListAsync();
        HasNext = PageNumber * 12 < count;
        return Page();
    }
}
