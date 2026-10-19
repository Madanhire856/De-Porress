using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
using SMS.Web.Pages.GeneratedNumbers;

namespace SMS.Web.Areas.Faith.Pages.Masses;

[Authorize]
public class DetailsModel(SMSDbContext db, WorkspaceAccess access, ICurrentUserService user) : PageModel
{
    public sealed class RosterInput
    {
        [Required] public string Participant { get; set; } = "";
        public int RoleId { get; set; } = (int)MinistryRole.READER;
    }
    public MassEvent Mass { get; private set; } = null!;
    [BindProperty] public RosterInput Input { get; set; } = new();
    public List<SelectListItem> Participants { get; private set; } = [];
    public bool CanEdit => SchoolMasses.CanEdit(access);

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        Mass = (await db.MassEvents.AsNoTracking().Include(x => x.MassRosters).ThenInclude(x => x.Student)
            .Include(x => x.MassRosters).ThenInclude(x => x.Staff).FirstOrDefaultAsync(x => x.Id == id, ct))!;
        if (Mass == null) return false;
        if (CanEdit)
        {
            var students = await db.Students.AsNoTracking().Where(x => x.EnrolmentStatusId == (int)EnrolmentStatus.ACTIVE)
                .OrderBy(x => x.Surname).ThenBy(x => x.Name).ToListAsync(ct);
            db.ApplyStudentNumbers(students);
            Participants = students.Select(x => new SelectListItem($"Student · {x.Surname} {x.Name} · {x.StudentNumber}", "student:" + x.Id)).ToList();
            Participants.AddRange(await db.Staff.AsNoTracking().Where(x => x.IsActive == true).OrderBy(x => x.Surname).ThenBy(x => x.Name)
                .Select(x => new SelectListItem("Staff · " + x.Surname + " " + x.Name + " · " + x.EcNumber, "staff:" + x.Id)).ToListAsync(ct));
        }
        return true;
    }
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        if (!SchoolMasses.CanView(access)) return Forbid();
        return await LoadAsync(id, ct) ? Page() : NotFound();
    }
    public async Task<IActionResult> OnPostAddAsync(Guid id, CancellationToken ct)
    {
        if (!CanEdit || !user.UserId.HasValue) return Forbid();
        if (!await LoadAsync(id, ct)) return NotFound();
        if (!Enum.IsDefined(typeof(MinistryRole), Input.RoleId)) ModelState.AddModelError("Input.RoleId", "Choose a ministry role.");
        var parts = (Input.Participant ?? "").Split(':');
        Guid participantId = Guid.Empty;
        if (parts.Length != 2 || !Guid.TryParse(parts.ElementAtOrDefault(1), out participantId)
            || !Participants.Any(x => x.Value == Input.Participant))
            ModelState.AddModelError("Input.Participant", "Choose an active student or staff member.");
        if (!ModelState.IsValid) return Page();
        var isStudent = parts[0] == "student";
        var exists = await db.MassRosters.AnyAsync(x => x.MassId == id && x.RoleId == Input.RoleId
            && (isStudent ? x.StudentId == participantId : x.StaffId == participantId), ct);
        if (exists) { ModelState.AddModelError("Input.Participant", "This person is already assigned to this ministry."); return Page(); }
        db.MassRosters.Add(new() { Id = Guid.NewGuid(), MassId = id, StudentId = isStudent ? participantId : null,
            StaffId = isStudent ? null : participantId, RoleId = Input.RoleId, CreatorId = user.UserId.Value, CreationDate = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        TempData["MassStatus"] = "Ministry assignment added.";
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostRemoveAsync(Guid id, Guid rosterId, CancellationToken ct)
    {
        if (!CanEdit || !user.UserId.HasValue) return Forbid();
        var row = await db.MassRosters.FirstOrDefaultAsync(x => x.Id == rosterId && x.MassId == id, ct);
        if (row == null) return NotFound();
        db.MassRosters.Remove(row); await db.SaveChangesAsync(ct);
        TempData["MassStatus"] = "Ministry assignment removed.";
        return RedirectToPage(new { id });
    }
}
