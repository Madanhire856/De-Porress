using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
namespace SMS.Web.Areas.Academics.Pages.Prefects;
[Authorize]
public class NominationsModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public int? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<PrefectNomination> Nominations { get; private set; } = [];
    public bool HasNext { get; private set; }
    public bool CanAppoint => access.Can(AccessRights.ViewPrefects) && PrefectAccess.CanManage(access, AccessRights.CreatePrefects);
    public bool CanReview => access.Can(AccessRights.ViewPrefects) && PrefectAccess.CanManage(access, AccessRights.EditPrefects);
    public static string StatusName(int status) => status switch { 0 => "Pending", 1 => "Appointed", 2 => "Under review", 5 => "Rejected", _ => "Closed" };
    public async Task<IActionResult> OnGetAsync()
    {
        if (!(PrefectAccess.CanManage(access, AccessRights.ViewPrefects)
            || (access.Workspace == SchoolWorkspace.Teacher && access.Can(AccessRights.ViewPrefects))
            || PrefectAccess.CanNominate(access))) return Forbid();
        var query = db.PrefectNominations.AsNoTracking();
        if (!PrefectAccess.CanManage(access, AccessRights.ViewPrefects)) query = query.Where(x => user.UserId.HasValue && x.NominatedByUserId == user.UserId);
        if (Status.HasValue) query = query.Where(x => x.StatusId == Status);
        if (!string.IsNullOrWhiteSpace(SearchTerm)) query = query.Where(x => (x.Student.Name + " " + x.Student.Surname).Contains(SearchTerm));
        var count = await query.CountAsync(); PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (count + 14) / 15));
        Nominations = await query.Include(x => x.Student).ThenInclude(x => x.Class).Include(x => x.NominatedByUser)
            .OrderByDescending(x => x.CreationDate).ThenBy(x => x.Id).Skip((PageNumber - 1) * 15).Take(15).ToListAsync();
        HasNext = PageNumber * 15 < count; return Page();
    }
    public async Task<IActionResult> OnPostReviewAsync(Guid id, string decision, string? notes)
    {
        if (!CanReview || !access.Can(AccessRights.ViewPrefects) || !user.UserId.HasValue) return Forbid();
        if (decision is not ("vetting" or "reject") || notes?.Length > 1000 || (decision == "reject" && string.IsNullOrWhiteSpace(notes))) return BadRequest("Enter a rejection reason of up to 1,000 characters.");
        bool changed = false;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); changed = false;
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var nomination = await db.PrefectNominations.SingleOrDefaultAsync(x => x.Id == id);
            if (nomination == null || nomination.StatusId is not (0 or 2)) return;
            nomination.StatusId = decision == "reject" ? (int)PrefectAppointmentStatus.REJECTED : (int)PrefectAppointmentStatus.VETTING_IN_PROGRESS;
            nomination.ReviewNotes = notes?.Trim(); nomination.ReviewedByUserId = user.UserId; nomination.ReviewedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(); await transaction.CommitAsync(); changed = true;
        });
        TempData["Success"] = changed ? "Nomination review saved." : "This nomination is no longer pending. Reload its current status.";
        return RedirectToPage();
    }
}
