using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.SubjectAssignments;

[Authorize]
public class IndexModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    public bool CanManage => access.Workspace == SchoolWorkspace.Administration && access.Can(AccessRights.EditSubjects);
    public List<SMS.Data.Staff> Teachers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!access.Can(AccessRights.ViewSubjects)) return Forbid();
        var teachers = db.Staff.AsNoTracking().Where(x => x.CategoryId == (int)StaffCategory.GENERAL_TEACHER
            || x.CategoryId == (int)StaffCategory.SENIOR_TEACHER || x.CategoryId == (int)StaffCategory.STUDENT_TEACHER
            || x.CategoryId == (int)StaffCategory.PRIEST);
        if (access.Workspace == SchoolWorkspace.Teacher)
            teachers = teachers.Where(x => user.UserId.HasValue && x.UserId == user.UserId);
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            teachers = teachers.Where(x => (x.Name + " " + x.Surname).Contains(search)
                || x.TeacherSubjects.Any(a => a.Subject.Name.Contains(search) || a.Subject.Code.Contains(search)));
        }
        Teachers = await teachers.Include(x => x.TeacherSubjects).ThenInclude(x => x.Subject)
            .OrderBy(x => x.Surname).ThenBy(x => x.Name).ToListAsync();
        return Page();
    }
}
