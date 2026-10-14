using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
namespace SMS.Web.Areas.Academics.Pages.Prefects;
[Authorize]
public class NominateModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty] public NominationInput Input { get; set; } = new();
    public List<SelectListItem> Students { get; private set; } = [];
    public List<SelectListItem> Posts { get; private set; } = [];
    public class NominationInput
    {
        [Required] public Guid? StudentId { get; set; }
        [Required] public int? PostTitleId { get; set; }
        [Required, StringLength(2000)] public string Reason { get; set; } = "";
    }
    public async Task<IActionResult> OnGetAsync()
    {
        if (!PrefectAccess.CanNominate(access)) return Forbid();
        await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!PrefectAccess.CanNominate(access) || !user.UserId.HasValue) return Forbid();
        if (!Input.PostTitleId.HasValue || !Enum.IsDefined(typeof(PrefectPostTitle), Input.PostTitleId.Value)) ModelState.AddModelError("Input.PostTitleId", "Select a valid post.");
        if (string.IsNullOrWhiteSpace(Input.Reason)) ModelState.AddModelError("Input.Reason", "Enter the nomination reason.");
        await LoadAsync();
        if (!ModelState.IsValid) return Page();
        string? error = null;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); error = null;
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await PrefectAccess.EligibleStudents(db, access, user.UserId).AnyAsync(x => x.Id == Input.StudentId))
            { error = "Choose an eligible active student. Teachers can nominate students in their assigned classes."; return; }
            var yearStart = new DateTime(DateTime.UtcNow.AddHours(2).Year, 1, 1).AddHours(-2);
            if (await db.PrefectNominations.AnyAsync(x => x.StudentId == Input.StudentId && x.PostTitleId == Input.PostTitleId
                && x.CreationDate >= yearStart && (x.StatusId == (int)PrefectAppointmentStatus.NOMINATED || x.StatusId == (int)PrefectAppointmentStatus.VETTING_IN_PROGRESS)))
            { error = "A pending nomination already exists for this student and post this year."; return; }
            db.PrefectNominations.Add(new() { Id = Guid.NewGuid(), StudentId = Input.StudentId!.Value, PostTitleId = Input.PostTitleId,
                Reason = Input.Reason.Trim(), NominatedByUserId = user.UserId!.Value, CreationDate = DateTime.UtcNow,
                StatusId = (int)PrefectAppointmentStatus.NOMINATED });
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        });
        if (error != null) { ModelState.AddModelError("", error); return Page(); }
        TempData["Success"] = "Nomination submitted for review."; return RedirectToPage("./Nominations");
    }
    private async Task LoadAsync()
    {
        Students = await PrefectAccess.EligibleStudents(db, access, user.UserId).OrderBy(x => x.Surname).ThenBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name + " " + x.Surname + (x.Class == null ? "" : " · " + x.Class.Name), x.Id.ToString())).ToListAsync();
        Posts = Enum.GetValues<PrefectPostTitle>().Select(x => new SelectListItem(x.ToDisplayName(), ((int)x).ToString())).ToList();
    }
}
