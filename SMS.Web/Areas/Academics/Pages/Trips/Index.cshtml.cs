using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;
using SMS.Web.Pages.Shared.Pagination;

namespace SMS.Web.Areas.Academics.Pages.Trips;

[Authorize]
public class IndexModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public int? StatusId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? GradeId { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
    public bool CanCreate => SchoolTrips.CanCreate(access);
    public bool CanEdit => SchoolTrips.CanEdit(access);
    public bool CanManage => SchoolTrips.CanViewDrafts(access);
    public List<SchoolTrip> Trips { get; private set; } = [];
    public List<Grade> Grades { get; private set; } = [];
    public PaginationInfo Pagination { get; private set; } = new();
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!SchoolTrips.CanView(access)) return Forbid();
        var query = SchoolTrips.Visible(db, CanManage).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) query = query.Where(x => x.Name.Contains(SearchTerm) || x.Destination.Contains(SearchTerm) || x.Purpose.Contains(SearchTerm));
        if (StatusId.HasValue) query = query.Where(x => x.StatusId == StatusId);
        if (GradeId.HasValue) query = query.Where(x => x.Grades.Any(g => g.GradeId == GradeId));
        var count = await query.CountAsync(ct);
        PageSize = Math.Clamp(PageSize, 1, 100); PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (int)Math.Ceiling(count / (double)PageSize)));
        Trips = await query.Include(x => x.Leader).Include(x => x.Grades).ThenInclude(x => x.Grade)
            .OrderByDescending(x => x.DepartureAt).ThenBy(x => x.Id).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync(ct);
        Grades = await db.Grades.AsNoTracking().OrderBy(x => x.GradeLevelId).ThenBy(x => x.Name).ToListAsync(ct);
        Pagination = new() { PageNumber = PageNumber, PageSize = PageSize, TotalCount = count, ItemName = "trips" };
        return Page();
    }
}
