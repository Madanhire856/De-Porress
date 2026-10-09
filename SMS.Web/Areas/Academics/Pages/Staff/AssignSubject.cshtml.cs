using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.Staff;

[Authorize]
public class AssignSubjectModel(SMSDbContext db, WorkspaceAccess access) : PageModel
{
    public SMS.Data.Staff StaffVM { get; private set; } = null!;
    public List<Subject> Subjects { get; private set; } = [];
    [BindProperty] public List<Guid> SelectedSubjectIds { get; set; } = [];
    [BindProperty] public string? PreviousSelection { get; set; }
    public bool CanManage => access.Workspace == SchoolWorkspace.Administration
        && access.Can(AccessRights.ViewSubjects) && access.Can(AccessRights.EditSubjects);

    public static string SelectionVersion(IEnumerable<Guid> ids) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(string.Join(",", ids.Distinct().OrderBy(x => x)))));

    public async Task<IActionResult> OnGetAsync(Guid staffId)
    {
        if (!CanManage) return Forbid();
        if (!await LoadAsync(staffId)) return NotFound();
        if (!IsEligible()) return BadRequest("Subject assignments require an active teaching staff member.");
        SelectedSubjectIds = await db.TeacherSubjects.Where(x => x.StaffId == staffId).Select(x => x.SubjectId).ToListAsync();
        PreviousSelection = SelectionVersion(SelectedSubjectIds);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid staffId)
    {
        if (!CanManage) return Forbid();
        if (!await LoadAsync(staffId)) return NotFound();
        if (!IsEligible()) return BadRequest("Subject assignments require an active teaching staff member.");
        var selected = SelectedSubjectIds.Distinct().ToHashSet();
        if (selected.Any(id => !Subjects.Any(x => x.Id == id)))
            ModelState.AddModelError("", "One or more selected subjects no longer exist. Reload and try again.");
        if (string.IsNullOrEmpty(PreviousSelection))
            ModelState.AddModelError("", "Reload the assignments before saving.");
        if (!ModelState.IsValid) return Page();
        string? error = null;
        try
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                error = null;
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var eligible = await db.Staff.AnyAsync(x => x.Id == staffId && x.IsActive == true
                    && (x.CategoryId == (int)StaffCategory.GENERAL_TEACHER || x.CategoryId == (int)StaffCategory.SENIOR_TEACHER
                        || x.CategoryId == (int)StaffCategory.STUDENT_TEACHER || x.CategoryId == (int)StaffCategory.PRIEST));
                if (!eligible) { error = "This staff member is no longer eligible for subject assignments."; return; }
                if (await db.Subjects.CountAsync(x => selected.Contains(x.Id)) != selected.Count)
                { error = "A selected subject has changed. Reload before saving."; return; }
                var existing = await db.TeacherSubjects.Where(x => x.StaffId == staffId).ToListAsync();
                if (SelectionVersion(existing.Select(x => x.SubjectId)) != PreviousSelection)
                { error = "These assignments were changed by someone else. Reload before saving."; return; }
                var retained = new HashSet<Guid>();
                foreach (var link in existing)
                    if (!selected.Contains(link.SubjectId) || !retained.Add(link.SubjectId)) db.TeacherSubjects.Remove(link);
                foreach (var id in selected.Except(retained))
                    db.TeacherSubjects.Add(new TeacherSubject { Id = Guid.NewGuid(), StaffId = staffId, SubjectId = id });
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
        catch (DbUpdateException) { error = "The subjects could not be saved. Reload and try again."; }
        if (error != null) { db.ChangeTracker.Clear(); ModelState.AddModelError("", error); return Page(); }
        TempData["SubjectsSaved"] = $"Subjects saved for {StaffVM.Name} {StaffVM.Surname}.";
        return RedirectToPage("/SubjectAssignments/Index", new { area = "Academics" });
    }

    private bool IsEligible() => StaffVM.IsActive == true &&
        (StaffVM.CategoryId == (int)StaffCategory.GENERAL_TEACHER || StaffVM.CategoryId == (int)StaffCategory.SENIOR_TEACHER
            || StaffVM.CategoryId == (int)StaffCategory.STUDENT_TEACHER || StaffVM.CategoryId == (int)StaffCategory.PRIEST);

    private async Task<bool> LoadAsync(Guid staffId)
    {
        StaffVM = (await db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.Id == staffId))!;
        if (StaffVM == null) return false;
        Subjects = await db.Subjects.AsNoTracking().OrderBy(x => x.CategoryId).ThenBy(x => x.Name).ToListAsync();
        return true;
    }
}
