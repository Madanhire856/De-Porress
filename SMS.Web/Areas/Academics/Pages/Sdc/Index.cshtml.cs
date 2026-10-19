using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;
namespace SMS.Web.Areas.Academics.Pages.Sdc;
[Authorize]
public class IndexModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? TermId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<SdcMember> Members { get; private set; } = [];
    public List<SelectListItem> Terms { get; private set; } = [];
    public int Total { get; private set; }
    public bool CanCreate => SdcAccess.CanCreateMembers(access);
    public bool CanEdit => SdcAccess.CanEditMembers(access);
    public SMS.Web.Pages.Shared.Pagination.PaginationInfo Pagination => new() { PageNumber = PageNumber, PageSize = 20, TotalCount = Total };
    public bool HasNext => PageNumber * 20 < Total;
    public DateTime Today => DateTime.UtcNow.AddHours(2).Date;
    public async Task<IActionResult> OnGetAsync()
    {
        if (!SdcAccess.CanViewMembers(access)) return Forbid();
        Terms = await db.SdcTerms.AsNoTracking().OrderByDescending(x => x.StartDate).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync();
        var query = db.SdcMembers.AsNoTracking();
        if (TermId.HasValue) query = query.Where(x => x.TermId == TermId);
        if (Status == "current") query = query.Where(x => x.IsActive && x.Term.IsActive && x.Term.StartDate <= Today && x.Term.EndDate >= Today);
        if (Status == "inactive") query = query.Where(x => !x.IsActive);
        if (!string.IsNullOrWhiteSpace(SearchTerm)) query = query.Where(x => x.Name.Contains(SearchTerm) || x.Position.Name.Contains(SearchTerm));
        Total = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (Total + 19) / 20));
        Members = await query.Include(x => x.Term).Include(x => x.Position).OrderByDescending(x => x.Term.StartDate)
            .ThenBy(x => x.Position.Name).ThenBy(x => x.Name).ThenBy(x => x.Id).Skip((PageNumber - 1) * 20).Take(20).ToListAsync();
        return Page();
    }
}
