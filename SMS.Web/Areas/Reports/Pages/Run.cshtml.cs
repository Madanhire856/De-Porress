using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Reports.Pages;

[Authorize]
public class RunModel(SMSDbContext db, SchoolReportService service, WorkspaceAccess access, ICurrentUserService user) : PageModel
{
    [BindProperty(SupportsGet = true)] public ReportFilters Filters { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;
    public ReportDefinition Definition { get; private set; } = null!;
    public ReportTable Data { get; private set; } = new([]);
    public List<string[]> Rows => IsPrint ? Data.Rows : Data.Rows.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
    public SMS.Web.Pages.Shared.Pagination.PaginationInfo Pagination => new() { PageNumber = PageNumber, PageSize = PageSize, TotalCount = Data.Rows.Count, PageUrl = page => Link(page: page) };
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Data.Rows.Count / (double)PageSize));
    public bool IsPrint { get; private set; }
    public bool CanPrint => access.Can(FeatureRights.PrintReports);
    public bool CanExport => access.Can(FeatureRights.ExportReports);
    public List<SelectListItem> Grades { get; private set; } = [];
    public List<SelectListItem> Classes { get; private set; } = [];
    public List<SelectListItem> Terms { get; private set; } = [];
    public DateTime GeneratedAt { get; } = DateTime.UtcNow.AddHours(2);
    public string GeneratedBy => user.Name ?? user.Email ?? "Staff";
    public bool Has(ReportFields field) => Definition.Fields.HasFlag(field);
    public string Link(string? handler = null, int? page = null)
    {
        var query = Request.Query.Where(x => !x.Key.Equals("handler", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals(nameof(PageNumber), StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("__Invariant", StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => x.Value.Select(value => new KeyValuePair<string, string?>(x.Key, value))).ToList();
        if (handler != null) query.Add(new("handler", handler));
        if (page.HasValue) query.Add(new(nameof(PageNumber), page.Value.ToString()));
        // Include effective date defaults in downloads and printed report URLs.
        void DateParameter(string name, DateOnly? value) {
            query.RemoveAll(x => x.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (value.HasValue) { query.Add(new(name, value.Value.ToString("yyyy-MM-dd"))); query.Add(new("__Invariant", name)); }
        }
        DateParameter("Filters.From", Filters.From); DateParameter("Filters.To", Filters.To);
        return $"{Request.PathBase}{Request.Path}{QueryString.Create(query)}";
    }
    public Task<IActionResult> OnGetAsync(string key, CancellationToken ct) => LoadAsync(key, false, false, ct);
    public Task<IActionResult> OnGetPrintAsync(string key, CancellationToken ct) => LoadAsync(key, true, false, ct);
    public Task<IActionResult> OnGetExportAsync(string key, CancellationToken ct) => LoadAsync(key, false, true, ct);
    private async Task<IActionResult> LoadAsync(string key, bool print, bool export, CancellationToken ct)
    {
        Definition = ReportCatalog.Find(key)!;
        if (Definition == null) return NotFound();
        if (!service.CanRun(key) || (print && !CanPrint) || (export && !CanExport)) return Forbid();
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        IsPrint = print; PageSize = Math.Clamp(PageSize, 1, 100);
        if (key is "attendance" or "collections" && Filters.From == null && Filters.To == null) {
            Filters.To = DateOnly.FromDateTime(GeneratedAt); Filters.From = Filters.To.Value.AddDays(-29);
        }
        if (ModelState.IsValid)
        {
            try { Data = await service.GenerateAsync(key, Filters, ct); }
            catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
            catch (ReportLimitException ex) { ModelState.AddModelError("", ex.Message); }
        }
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        if (export) {
            if (!ModelState.IsValid) return BadRequest("The report filters are invalid or too broad. Review them on the report page.");
            return File(ReportCsv.Encode(Data), "text/csv; charset=utf-8", $"school-{key}-{GeneratedAt:yyyyMMdd}.csv");
        }
        if (Has(ReportFields.Grade)) {
            var grades = db.Grades.AsNoTracking();
            if (access.Workspace == SchoolWorkspace.Teacher) { var classGrades = service.Classes().Select(x => x.GradeId); grades = grades.Where(x => classGrades.Contains(x.Id)); }
            Grades = await grades.OrderBy(x => x.GradeLevelId).ThenBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync(ct);
        }
        if (Has(ReportFields.Class)) Classes = await service.Classes().OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync(ct);
        if (Has(ReportFields.Term)) Terms = key == "sdc"
            ? await db.SdcTerms.AsNoTracking().OrderByDescending(x => x.StartDate).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync(ct)
            : await db.Terms.AsNoTracking().OrderByDescending(x => x.StartDate).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync(ct);
        return Page();
    }
}
