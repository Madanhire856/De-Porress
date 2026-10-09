using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using SMS.Lib;
using SMS.Web.Services;

var checkedCases = 0;
void Check(bool condition, string message)
{
    checkedCases++;
    if (!condition) throw new Exception(message);
}
WorkspaceAccess Access(UserRole roles, long rights = ~0L, bool authenticated = true) => new(new FakeUser((int)roles, rights, authenticated));
Check(Access(UserRole.BURSER, 0).CanViewDashboard, "Bursar dashboard opens without any rights");
Check(Access(UserRole.BURSER, 1L).CanViewDashboard, "Retired dashboard bit has no effect on access");
Check(Access(UserRole.CLASS_TEACHER, 0).CanViewDashboard, "Teacher dashboard is role-based");
Check(Access(UserRole.ADMIN, 0).CanViewDashboard, "Administration dashboard is role-based");
Check(!Access(UserRole.PARENT).CanViewDashboard, "Unsupported roles do not get staff dashboards");
Check(!Access(UserRole.BURSER, 0, false).CanViewDashboard, "Anonymous users cannot get staff dashboards");
var teacher = Access(UserRole.CLASS_TEACHER);
var bursar = Access(UserRole.BURSER);
var admin = Access(UserRole.ADMIN);
var configuredTeacherRights = AccessRights.ViewStaff | AccessRights.ViewGrades | AccessRights.ViewClasses
    | AccessRights.ViewSubjects | AccessRights.ViewTerms | AccessRights.ViewTimetable | AccessRights.ViewLearners
    | AccessRights.MarkAttendance | AccessRights.GenerateReportCards | AccessRights.ViewHouses | AccessRights.ViewVillages
    | AccessRights.ViewAllBursarsCashUp | AccessRights.ViewSports | AccessRights.NominatePrefects | AccessRights.ViewPrefects;
var configuredTeacher = Access(UserRole.CLASS_TEACHER, (long)configuredTeacherRights);
Check(configuredTeacher.Navigation.Count() == 13, "The supplied teacher rights expose all 13 implemented teacher tools including class and subject assignments");
Check(configuredTeacher.CanOpen("Academics", "/Staff/Index"), "Granted teacher staff viewing is available");
Check(configuredTeacher.CanOpen("Config", "/Grades/Index"), "Granted teacher grade viewing is available");
Check(configuredTeacher.CanOpen("Config", "/Villages/Index"), "Granted teacher village viewing is available");
Check(!Access(UserRole.CLASS_TEACHER, (long)AccessRights.NominatePrefects).CanOpen("Academics", "/Staff/Index"), "Nomination permission cannot grant staff viewing");
Check(teacher.CanOpen("Academics", "/Students/Index"), "Teacher with learner right sees students");
Check(!teacher.CanOpen("Finance", "/Fees/Index"), "Teacher must not see finance even with all rights");
Check(!teacher.IsRelevantPage("Finance", "/Fees/RecordPayment"), "Teacher cannot open a finance action directly");
Check(!teacher.CanOpen("Config", "/Users/Index"), "Teacher cannot manage system users");
Check(bursar.CanOpen("Finance", "/Fees/Index"), "Bursar sees finance");
Check(!bursar.CanOpen("Academics", "/Students/Index"), "Bursar does not receive the teaching directory");
Check(!bursar.IsRelevantPage("Sports", "/Sporting/Assign"), "Bursar cannot open teaching sports actions directly");
Check(!bursar.IsRelevantPage("Config", "/Classes/Upsert"), "Bursar cannot configure teaching classes");
Check(admin.CanOpen("Finance", "/Fees/Index") && admin.CanOpen("Academics", "/Students/Index"), "Administration spans both workspaces");
Check(!Access(UserRole.CLASS_TEACHER, 0).Navigation.Any(), "No rights means no modules");
Check(!Access(UserRole.BURSER, (long)AccessRights.ViewLearners).Navigation.Any(), "Irrelevant rights do not expose modules");
Check(Access(UserRole.CLASS_TEACHER, (long)AccessRights.ViewLearners).Navigation.Single().Title == "Students", "Only granted modules are visible");
Check(!Access(UserRole.BURSER, 0).CanOpen("Finance", "/Fees/Index"), "Role alone does not grant finance access");
Check(WorkspaceAccess.Resolve((int)(UserRole.ADMIN | UserRole.BURSER)) == SchoolWorkspace.Administration, "Admin wins for combined roles");
Check(WorkspaceAccess.Resolve((int)(UserRole.STAFF | UserRole.BURSER)) == SchoolWorkspace.Bursar, "Bursar wins over staff for combined roles");
Check(WorkspaceAccess.Resolve((int)UserRole.HEAD) == SchoolWorkspace.Administration, "Head uses administration with assigned rights");
Check(!Access(UserRole.PARENT).Navigation.Any(), "Parent receives no staff workspace");
Check(!Access(UserRole.ADMIN, ~0L, false).Navigation.Any(), "Anonymous users receive no navigation");
Check(!teacher.CanOpen("", "/Timetable/Index"), "Unimplemented routes are not displayed");
Check(bursar.CanOpen("Config", "/Terms/Index"), "Shared term calendar is relevant to bursar");
Check(teacher.CanOpen("Config", "/Terms/Index"), "Shared term calendar is relevant to teacher");
async Task CheckFilter(WorkspaceAccess access, string area, string page, bool allow, bool authenticated = true)
{
    var http = new DefaultHttpContext();
    http.User = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "checks" : null));
    var routes = new RouteData();
    routes.Values["area"] = area;
    routes.Values["page"] = page;
    var pageContext = new PageContext { HttpContext = http, RouteData = routes, ActionDescriptor = new CompiledPageActionDescriptor() };
    var executing = new PageHandlerExecutingContext(pageContext, [], null!, new Dictionary<string, object?>(), new object());
    var called = false;
    await new WorkspacePageFilter(access).OnPageHandlerExecutionAsync(executing, () =>
    {
        called = true;
        return Task.FromResult(new PageHandlerExecutedContext(pageContext, [], null, new object()));
    });
    Check(called == allow, $"Page filter access for {area}{page}");
    if (!allow) Check(authenticated ? executing.Result is ForbidResult : executing.Result is ChallengeResult, "Expected access denial result");
}
await CheckFilter(teacher, "Finance", "/Fees/Index", false);
await CheckFilter(teacher, "Finance", "/Fees/RecordPayment", false);
await CheckFilter(bursar, "Academics", "/Students/Index", false);
await CheckFilter(teacher, "Config", "/Classes/Index", true);
await CheckFilter(teacher, "Academics", "/ClassTeacherAssignments/Index", true);
await CheckFilter(admin, "Academics", "/ClassTeacherAssignments/Index", true);
await CheckFilter(bursar, "Academics", "/ClassTeacherAssignments/Index", false);
await CheckFilter(bursar, "Academics", "/SubjectAssignments/Index", false);
await CheckFilter(teacher, "Academics", "/SubjectAssignments/Index", true);
await CheckFilter(Access(UserRole.CLASS_TEACHER, 0), "Academics", "/SubjectAssignments/Index", false);
await CheckFilter(Access(UserRole.CLASS_TEACHER, 0), "Academics", "/ClassTeacherAssignments/Index", false);
await CheckFilter(configuredTeacher, "Academics", "/Staff/Index", true);
await CheckFilter(configuredTeacher, "Config", "/Grades/Details", true);
await CheckFilter(configuredTeacher, "Config", "/Villages/Index", true);
await CheckFilter(Access(UserRole.CLASS_TEACHER, 0), "Config", "/Classes/Index", false);
await CheckFilter(admin, "Config", "/Users/Index", true);
await CheckFilter(admin, "Config", "/Users/Index", false, false);
await CheckFilter(teacher, "", "/Index", true);
using (var db = new SMS.Data.SMSDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SMS.Data.SMSDbContext>()
    .UseSqlServer("Server=localhost;Database=UnusedChecks;Integrated Security=True;TrustServerCertificate=True").Options))
{
    foreach (var role in new[] { UserRole.ADMIN, UserRole.HEAD, UserRole.DEPUTY_HEAD })
    {
        var user = new FakeUser((int)role, (long)(AccessRights.ViewClasses | AccessRights.EditClasses), true);
        var page = new SMS.Web.Areas.Academics.Pages.ClassTeacherAssignments.IndexModel(db, user, new WorkspaceAccess(user));
        Check(page.CanManage, $"{role} can manage assignments with class rights");
    }
    foreach (var role in new[] { UserRole.CLASS_TEACHER, UserRole.BURSER, UserRole.PARENT })
    {
        var user = new FakeUser((int)role, ~0L, true);
        var page = new SMS.Web.Areas.Academics.Pages.ClassTeacherAssignments.IndexModel(db, user, new WorkspaceAccess(user));
        Check(!page.CanManage, $"{role} cannot manage assignments even with all rights");
        Check(await page.OnPostAsync() is ForbidResult, $"{role} direct POST rejected before database access");
    }
    var viewUser = new FakeUser((int)UserRole.ADMIN, (long)AccessRights.ViewClasses, true);
    var viewPage = new SMS.Web.Areas.Academics.Pages.ClassTeacherAssignments.IndexModel(db, viewUser, new WorkspaceAccess(viewUser));
    Check(await viewPage.OnPostAsync() is ForbidResult, "ViewClasses alone cannot save assignments");
    foreach (var role in new[] { UserRole.CLASS_TEACHER, UserRole.BURSER, UserRole.PARENT })
    {
        var user = new FakeUser((int)role, ~0L, true);
        var page = new SMS.Web.Areas.Academics.Pages.Staff.AssignSubjectModel(db, new WorkspaceAccess(user));
        Check(await page.OnGetAsync(Guid.NewGuid()) is ForbidResult, $"{role} cannot open the subject editor");
        Check(await page.OnPostAsync(Guid.NewGuid()) is ForbidResult, $"{role} cannot submit subject assignments with broad rights");
    }
    var subjectViewer = new FakeUser((int)UserRole.ADMIN, (long)AccessRights.ViewSubjects, true);
    var subjectPage = new SMS.Web.Areas.Academics.Pages.Staff.AssignSubjectModel(db, new WorkspaceAccess(subjectViewer));
    Check(await subjectPage.OnPostAsync(Guid.NewGuid()) is ForbidResult, "ViewSubjects alone cannot edit assignments");
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();
    Check(SMS.Web.Areas.Academics.Pages.Staff.AssignSubjectModel.SelectionVersion([first, second, first])
        == SMS.Web.Areas.Academics.Pages.Staff.AssignSubjectModel.SelectionVersion([second, first]),
        "Selection version ignores checkbox order and duplicates");
    Check(SMS.Web.Areas.Academics.Pages.Staff.AssignSubjectModel.SelectionVersion([first])
        != SMS.Web.Areas.Academics.Pages.Staff.AssignSubjectModel.SelectionVersion([first, second]),
        "Selection version detects changed assignments");
    var entity = db.Model.FindEntityType(typeof(SMS.Data.ClassTeacherAssignment))!;
    Check(entity.GetIndexes().Any(x => x.IsUnique && x.GetFilter() == "[EndedAt] IS NULL"
        && x.Properties.Select(p => p.Name).SequenceEqual(new[] { "ClassId", "AcademicYear" })),
        "Database enforces one open assignment per class and year");
    Check(entity.GetForeignKeys().All(x => x.DeleteBehavior == Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict),
        "Assignment history cannot be silently cascade deleted");
    var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
    var script = migrator.GenerateScript("20260927183412_nullable teacher class Teacher Id in Class", "20261009055359_ClassTeacherAssignments");
    Check(script.Contains("INSERT INTO ClassTeacherAssignment") && script.Contains("WHERE ClassTeacherId IS NOT NULL"),
        "Migration retains existing teacher assignments");
}
Console.WriteLine($"Passed {checkedCases} workspace and assignment checks.");

sealed class FakeUser(int role, long rights, bool authenticated) : ICurrentUserService
{
    public string? Email => "check@example.test";
    public string? Name => "Check user";
    public string? AzureId => null;
    public bool IsAuthenticated => authenticated;
    public Guid? UserId => Guid.Empty;
    public int? RoleId => role;
    public Guid? GroupId => Guid.Empty;
    public long Rights => rights;
    public bool HasRight(AccessRights right) => (rights & (long)right) == (long)right;
    public Task<Guid> GetUserIdAsync() => Task.FromResult(Guid.Empty);
    public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult<Guid?>(Guid.Empty);
}


