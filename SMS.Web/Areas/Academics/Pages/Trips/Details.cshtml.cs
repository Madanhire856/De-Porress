using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.Trips;

[Authorize]
public class DetailsModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    public SchoolTrip Trip { get; private set; } = null!;
    public bool CanCreate => SchoolTrips.CanCreate(access);
    public bool CanEdit => SchoolTrips.CanEdit(access);
    public bool CanManage => SchoolTrips.CanViewDrafts(access);
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        if (!SchoolTrips.CanView(access)) return Forbid();
        Trip = (await SchoolTrips.Visible(db, CanManage).AsNoTracking().Include(x => x.Leader).Include(x => x.Currency)
            .Include(x => x.Grades).ThenInclude(x => x.Grade).SingleOrDefaultAsync(x => x.Id == id, ct))!;
        return Trip == null ? NotFound() : Page();
    }
}
