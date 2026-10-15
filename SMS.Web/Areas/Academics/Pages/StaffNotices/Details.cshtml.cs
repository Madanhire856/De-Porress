using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.StaffNotices;
[Authorize]
public class DetailsModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    public Announcement Notice { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!SchoolNotices.CanReadStaff(access)) return Forbid();
        Notice = (await SchoolNotices.StaffPublished(db).SingleOrDefaultAsync(x => x.Id == id))!;
        return Notice == null ? NotFound() : Page();
    }
}
