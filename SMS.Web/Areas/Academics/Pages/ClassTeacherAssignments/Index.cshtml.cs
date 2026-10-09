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

namespace SMS.Web.Areas.Academics.Pages.ClassTeacherAssignments;

[Authorize]
public class IndexModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    public int CurrentYear => DateTime.UtcNow.AddHours(2).Year;
    [BindProperty(SupportsGet = true), Range(2000, 2100)]
    public int? Year { get; set; }
    [BindProperty] public AssignmentInput Input { get; set; } = new();
    public bool CanManage => access.Workspace == SchoolWorkspace.Administration && access.Can(AccessRights.EditClasses);
    public List<SelectListItem> Classes { get; private set; } = [];
    public List<SelectListItem> Teachers { get; private set; } = [];
    public List<ClassRow> Rows { get; private set; } = [];
    public List<ClassTeacherAssignment> History { get; private set; } = [];

    public class AssignmentInput
    {
        [Required] public Guid? ClassId { get; set; }
        public Guid? StaffId { get; set; }
        public Guid? PreviousAssignmentId { get; set; }
    }
    public record ClassRow(Guid Id, string Name, string Grade, Guid? AssignmentId, string? Teacher);

    public async Task<IActionResult> OnGetAsync(Guid? classId)
    {
        if (!access.Can(AccessRights.ViewClasses)) return Forbid();
        Year ??= CurrentYear;
        // The editor input is only validated on POST.
        ModelState.Remove("Input.ClassId");
        if (Year < 2000 || Year > 2100) return BadRequest();
        await LoadAsync();
        if (CanManage && classId.HasValue)
        {
            var row = Rows.FirstOrDefault(x => x.Id == classId);
            if (row == null) return NotFound();
            Input.ClassId = row.Id;
            Input.PreviousAssignmentId = row.AssignmentId;
            Input.StaffId = History.FirstOrDefault(x => x.Id == row.AssignmentId)?.StaffId;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!CanManage || !user.UserId.HasValue || !access.Can(AccessRights.ViewClasses)) return Forbid();
        Year ??= CurrentYear;
        if (Year != CurrentYear)
            ModelState.AddModelError("Year", "Assignments can only be changed for the current academic year.");
        if (Input.ClassId == Guid.Empty)
            ModelState.AddModelError("Input.ClassId", "Select a class.");
        if (Input.StaffId == Guid.Empty)
            ModelState.AddModelError("Input.StaffId", "Select a teacher or leave the class unassigned.");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }

        string? error = null;
        try
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                error = null;
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var classroom = await db.Classes.SingleOrDefaultAsync(x => x.Id == Input.ClassId);
                if (classroom == null) { error = "The selected class no longer exists."; return; }
                var previous = await db.ClassTeacherAssignments.SingleOrDefaultAsync(x =>
                    x.ClassId == classroom.Id && x.AcademicYear == Year && x.EndedAt == null);
                if (previous?.Id != Input.PreviousAssignmentId)
                { error = "This assignment has changed. Reload the page before saving again."; return; }
                if (Input.StaffId.HasValue && !await db.Staff.AnyAsync(x => x.Id == Input.StaffId && x.IsActive == true
                    && (x.CategoryId == (int)StaffCategory.GENERAL_TEACHER || x.CategoryId == (int)StaffCategory.SENIOR_TEACHER
                        || x.CategoryId == (int)StaffCategory.STUDENT_TEACHER || x.CategoryId == (int)StaffCategory.PRIEST)))
                { error = "Select an active teaching staff member."; return; }
                if (previous?.StaffId == Input.StaffId) { await transaction.CommitAsync(); return; }
                var now = DateTime.UtcNow;
                if (previous != null)
                {
                    previous.EndedAt = now;
                    // Close the previous row before inserting its replacement (filtered unique index).
                    await db.SaveChangesAsync();
                }
                if (Input.StaffId.HasValue)
                    db.ClassTeacherAssignments.Add(new ClassTeacherAssignment
                    {
                        Id = Guid.NewGuid(), ClassId = classroom.Id, StaffId = Input.StaffId.Value,
                        AcademicYear = Year!.Value, AssignedAt = now, AssignedById = user.UserId!.Value
                    });
                classroom.ClassTeacherId = Input.StaffId;
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
        catch (DbUpdateException)
        {
            error = "The assignment could not be saved. Reload and try again.";
        }
        if (error != null)
        {
            db.ChangeTracker.Clear();
            ModelState.AddModelError("", error);
            await LoadAsync();
            return Page();
        }
        TempData["AssignmentSaved"] = "Class teacher assignment saved.";
        return RedirectToPage(new { Year });
    }

    private async Task LoadAsync()
    {
        var assignments = db.ClassTeacherAssignments.AsNoTracking().Where(x => x.AcademicYear == Year);
        var classes = db.Classes.AsNoTracking();
        if (access.Workspace == SchoolWorkspace.Teacher)
        {
            assignments = assignments.Where(x => user.UserId.HasValue && x.Staff.UserId == user.UserId);
            classes = classes.Where(x => assignments.Any(a => a.ClassId == x.Id && a.EndedAt == null));
        }
        Rows = await classes.OrderBy(x => x.Grade.Name).ThenBy(x => x.Name)
            .Select(x => new ClassRow(x.Id, x.Name, x.Grade.Name,
                assignments.Where(a => a.ClassId == x.Id && a.EndedAt == null).Select(a => (Guid?)a.Id).FirstOrDefault(),
                assignments.Where(a => a.ClassId == x.Id && a.EndedAt == null)
                    .Select(a => a.Staff.Name + " " + a.Staff.Surname).FirstOrDefault())).ToListAsync();
        History = await assignments.Include(x => x.Class).Include(x => x.Staff).Include(x => x.AssignedBy)
            .OrderByDescending(x => x.AssignedAt).ToListAsync();
        if (CanManage)
        {
            Classes = Rows.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
            Teachers = await db.Staff.AsNoTracking().Where(x => x.IsActive == true
                && (x.CategoryId == (int)StaffCategory.GENERAL_TEACHER || x.CategoryId == (int)StaffCategory.SENIOR_TEACHER
                    || x.CategoryId == (int)StaffCategory.STUDENT_TEACHER || x.CategoryId == (int)StaffCategory.PRIEST))
                .OrderBy(x => x.Surname).ThenBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name + " " + x.Surname, x.Id.ToString())).ToListAsync();
        }
    }
}
