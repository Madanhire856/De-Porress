using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Areas.Academics.Pages.Prefects.ViewModels;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Services;
namespace SMS.Web.Areas.Academics.Pages.Prefects;
[Authorize]
public class UpsertModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty] public PrefectVM Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public Guid? NominationId { get; set; }
    public List<SelectListItem> StudentOptions { get; private set; } = [];
    public List<SelectListItem> PostTitleOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];
    public List<SelectListItem> NominatorOptions { get; private set; } = [];
    public bool IsEdit => Input.Id != Guid.Empty;
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!PrefectAccess.CanManage(access, id.HasValue ? AccessRights.EditPrefects : AccessRights.CreatePrefects)) return Forbid();
        if (id.HasValue)
        {
            var appointment = await db.Prefects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (appointment == null) return NotFound();
            Input = new() { Id = appointment.Id, StudentId = appointment.StudentId, PostTitleId = appointment.PostTitleId,
                StatusId = appointment.StatusId, StartDate = appointment.StartDate, EndDate = appointment.EndDate, NominatorId = appointment.NominatorId };
        }
        if (NominationId.HasValue)
        {
            if (id.HasValue || !access.Can(AccessRights.ViewPrefects)) return Forbid();
            var nomination = await db.PrefectNominations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == NominationId);
            if (nomination == null) return NotFound();
            if (nomination.PrefectId.HasValue) return RedirectToPage("./Details", new { id = nomination.PrefectId });
            if (nomination.StatusId is not (0 or 2)) return BadRequest("This nomination is no longer pending.");
            Input.StudentId = nomination.StudentId; Input.PostTitleId = nomination.PostTitleId ?? 0;
            Input.StatusId = (int)PrefectStatus.ACTIVE;
        }
        await LoadDropdownsAsync(); return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (!PrefectAccess.CanManage(access, id.HasValue ? AccessRights.EditPrefects : AccessRights.CreatePrefects) || !user.UserId.HasValue) return Forbid();
        if (Input.Id != (id ?? Guid.Empty)) return BadRequest();
        if (NominationId.HasValue && (id.HasValue || !access.Can(AccessRights.ViewPrefects))) return Forbid();
        if (NominationId.HasValue && Input.StatusId != (int)PrefectStatus.ACTIVE)
            ModelState.AddModelError("Input.StatusId", "A nomination approval must create an active appointment.");
        var request = new PrefectAppointments.Request(Input.StudentId, Input.PostTitleId, Input.StatusId, Input.StartDate, Input.EndDate, Input.NominatorId);
        var error = PrefectAppointments.Validate(request);
        if (error != null) ModelState.AddModelError("", error);
        if (!ModelState.IsValid) { await LoadDropdownsAsync(); return Page(); }
        try
        {
            var result = await new PrefectAppointments(db).SaveAsync(id, request, user.UserId.Value, NominationId);
            if (result.Missing) return NotFound();
            if (result.Error != null) { ModelState.AddModelError("", result.Error); await LoadDropdownsAsync(); return Page(); }
            TempData["Success"] = NominationId.HasValue ? "Nomination approved and prefect appointed." : "Prefect appointment saved.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "The appointment could not be saved. Reload and check the appointment details.");
            await LoadDropdownsAsync(); return Page();
        }
    }
    private async Task LoadDropdownsAsync()
    {
        var students = await db.Students.AsNoTracking().Include(x => x.Class)
            .Where(x => x.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE || x.Id == Input.StudentId)
            .OrderBy(x => x.Surname).ThenBy(x => x.Name).ToListAsync();
        db.ApplyStudentNumbers(students);
        StudentOptions = students.Select(x => new SelectListItem($"{x.Name} {x.Surname} ({x.StudentNumber}) · {x.Class?.Name ?? "No class"}", x.Id.ToString())).ToList();
        PostTitleOptions = Enum.GetValues<PrefectPostTitle>().Select(x => new SelectListItem(x.ToDisplayName(), ((int)x).ToString())).ToList();
        StatusOptions = Enum.GetValues<PrefectStatus>().Select(x => new SelectListItem(x.ToDisplayName(), ((int)x).ToString())).ToList();
        if (NominationId.HasValue) StatusOptions = StatusOptions.Where(x => x.Value == ((int)PrefectStatus.ACTIVE).ToString()).ToList();
        NominatorOptions = await db.Staff.AsNoTracking().Where(x => x.IsActive == true || x.Id == Input.NominatorId)
            .OrderBy(x => x.Surname).Select(x => new SelectListItem(x.Name + " " + x.Surname, x.Id.ToString())).ToListAsync();
    }
}
