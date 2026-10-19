using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMS.Web.Services;

namespace SMS.Web.Areas.Reports.Pages;

[Authorize]
public class IndexModel(WorkspaceAccess access) : PageModel
{
    public List<ReportDefinition> Reports { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? Group { get; set; }
    public List<string> Groups { get; private set; } = [];
    public IActionResult OnGet()
    {
        if (!access.CanOpen("Reports", "/Index")) return Forbid();
        Response.Headers.CacheControl = "no-store";
        Reports = ReportCatalog.All.Where(x => ReportCatalog.Allowed(x, access)).ToList();
        Groups = Reports.Select(x => x.Group).Distinct().OrderBy(x => x).ToList();
        if (!string.IsNullOrEmpty(Group)) Reports = Reports.Where(x => x.Group == Group).ToList();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) Reports = Reports.Where(x => (x.Title + " " + x.Description).Contains(SearchTerm.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        return Page();
    }
}
