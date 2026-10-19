using Microsoft.EntityFrameworkCore;
using SMS.Lib;

namespace SMS.Web.Services;

public sealed partial class SchoolReportService
{
    private async Task<ReportTable> StaffReport(ReportFilters f, CancellationToken ct)
    {
        var q = Dates(db.Staff.AsNoTracking(), x => x.DateJoined, f);
        if (f.Status.HasValue) q = q.Where(x => x.IsActive == (f.Status == 1));
        var rows = await Load(q.OrderBy(x => x.Surname).ThenBy(x => x.Name).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["EC number", "Staff member", "Category", "Joined", "Status"]);
        foreach (var x in rows) result.Add(Text(x.EcNumber), StaffName(x), ((StaffCategory)x.CategoryId).ToDisplayName(), Date(x.DateJoined), x.IsActive == true ? "Active" : x.IsActive == false ? "Inactive" : "Unspecified");
        result.Metrics.Add(new("Active staff", rows.Count(x => x.IsActive == true).ToString()));
        return result;
    }
    private async Task<ReportTable> ClassTeachers(ReportFilters f, CancellationToken ct)
    {
        var classIds = Classes().Select(x => x.Id);
        var q = Dates(db.ClassTeacherAssignments.AsNoTracking().Where(x => classIds.Contains(x.ClassId)), x => x.AssignedAt, f, true);
        if (access.Workspace == SchoolWorkspace.Teacher) q = q.Where(x => user.UserId.HasValue && x.Staff.UserId == user.UserId);
        if (f.ClassId.HasValue) q = q.Where(x => x.ClassId == f.ClassId);
        if (f.GradeId.HasValue) q = q.Where(x => x.Class.GradeId == f.GradeId);
        if (f.Status.HasValue) q = q.Where(x => (x.EndedAt == null) == (f.Status == 1));
        var rows = await Load(q.Include(x => x.Class).ThenInclude(x => x.Grade).Include(x => x.Staff).OrderByDescending(x => x.AcademicYear).ThenBy(x => x.Class.Name).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Academic year", "Grade", "Class", "Teacher", "Assigned (Zimbabwe)", "Ended (Zimbabwe)", "Status"]);
        foreach (var x in rows) result.Add(x.AcademicYear.ToString(), x.Class.Grade.Name, x.Class.Name, StaffName(x.Staff), Date(x.AssignedAt.AddHours(2)), x.EndedAt.HasValue ? Date(x.EndedAt.Value.AddHours(2)) : "—", x.EndedAt.HasValue ? "Ended" : "Active");
        if (access.Workspace == SchoolWorkspace.Teacher) result.Notes.Add("Limited to your assignments in classes currently assigned to you.");
        return result;
    }
    private async Task<ReportTable> Subjects(CancellationToken ct)
    {
        var q = db.TeacherSubjects.AsNoTracking();
        if (access.Workspace == SchoolWorkspace.Teacher) q = q.Where(x => user.UserId.HasValue && x.Staff.UserId == user.UserId);
        var rows = await Load(q.Include(x => x.Subject).Include(x => x.Staff).OrderBy(x => x.Subject.Name).ThenBy(x => x.Staff.Surname).ThenBy(x => x.Id), ct);
        var result = new ReportTable(["Subject", "Subject code", "Teacher", "EC number"]);
        foreach (var x in rows) result.Add(x.Subject.Name, x.Subject.Code, StaffName(x.Staff), Text(x.Staff.EcNumber));
        if (access.Workspace == SchoolWorkspace.Teacher) result.Notes.Add("Limited to your subject assignments.");
        return result;
    }
    private async Task<ReportTable> Users(ReportFilters f, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking();
        if (f.Status.HasValue) q = q.Where(x => x.IsActive == (f.Status == 1));
        var rows = await Load(q.OrderBy(x => x.Email).ThenBy(x => x.Id).Select(x => new {
            x.Name, x.Email, x.RoleId, GroupName = x.Group == null ? null : x.Group.Name, x.IsActive, x.IsEmailConfirmed, x.LastLoginDate
        }), ct);
        var result = new ReportTable(["Name", "Email", "Role", "Group", "Status", "Email verified", "Last sign-in (Zimbabwe)"]);
        foreach (var x in rows) result.Add(Text(x.Name), x.Email, ((UserRole)x.RoleId).ToString().Replace('_', ' '), Text(x.GroupName), x.IsActive ? "Active" : "Inactive", x.IsEmailConfirmed ? "Yes" : "No", x.LastLoginDate.HasValue ? DateTimeText(x.LastLoginDate.Value.AddHours(2)) : "—");
        return result;
    }
}
