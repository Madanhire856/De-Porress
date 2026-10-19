using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;
using SMS.Web.Pages.Shared.Pagination;

namespace SMS.Web.Areas.Faith.Pages.Masses;

[Authorize]
public class IndexModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    [BindProperty(SupportsGet = true)] public bool Past { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
    public List<MassEvent> Masses { get; private set; } = [];
    public PaginationInfo Pagination { get; private set; } = new();
    public bool CanCreate => SchoolMasses.CanCreate(access);
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!SchoolMasses.CanView(access)) return Forbid();
        var today = DateTime.UtcNow.AddHours(2).Date;
        var query = db.MassEvents.AsNoTracking().Where(x => Past ? x.DateTime < today : x.DateTime >= today);
        var ordered = Past ? query.OrderByDescending(x => x.DateTime).ThenBy(x => x.Id) : query.OrderBy(x => x.DateTime).ThenBy(x => x.Id);
        PageSize = Math.Clamp(PageSize, 1, 100);
        var count = await query.CountAsync(ct);
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (int)Math.Ceiling(count / (double)PageSize)));
        Masses = await ordered.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync(ct);
        Pagination = new() { PageNumber = PageNumber, PageSize = PageSize, TotalCount = count, ItemName = "Masses" };
        return Page();
    }
}
