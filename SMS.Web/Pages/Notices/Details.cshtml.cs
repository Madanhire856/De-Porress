using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;

namespace SMS.Web.Pages.Notices;
[AllowAnonymous]
public class DetailsModel(SMSDbContext db) : PageModel
{
    public Announcement Notice { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Notice = (await SchoolNotices.Published(db).SingleOrDefaultAsync(x => x.Id == id))!;
        return Notice == null ? NotFound() : Page();
    }
}
