using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;

namespace SMS.Web.Pages.Notices;
[AllowAnonymous]
public class IndexModel(SMSDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<Announcement> Notices { get; private set; } = [];
    public bool HasNext { get; private set; }
    public async Task OnGetAsync()
    {
        var query = SchoolNotices.Published(db);
        var count = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (count + 11) / 12));
        Notices = await query.OrderByDescending(x => x.CreationDate).ThenByDescending(x => x.Id)
            .Skip((PageNumber - 1) * 12).Take(12).ToListAsync();
        HasNext = PageNumber * 12 < count;
    }
}
