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
WorkspaceAccess Access(UserRole roles, long rights = ~0L, bool authenticated = true, bool nominate = false) => new(new FakeUser((int)roles, rights, authenticated, nominate));
var fullMonth = WeeklyCollectionPeriods.Create(new(2026, 10, 1), new(2026, 10, 31));
Check(fullMonth.Count == 5 && fullMonth[4].Start.Day == 29 && fullMonth[4].End.Day == 31, "31-day month has five bounded weeks");
var currentMonth = WeeklyCollectionPeriods.Create(new(2026, 10, 1), new(2026, 10, 9));
Check(currentMonth.Count == 2 && currentMonth[1].Start.Day == 8 && currentMonth[1].End.Day == 9, "No future week or future day appears");
var february = WeeklyCollectionPeriods.Create(new(2027, 2, 1), new(2027, 2, 28));
Check(february.Count == 4, "28-day month has four weeks");
var leapMonth = WeeklyCollectionPeriods.Create(new(2028, 2, 1), new(2028, 2, 29));
Check(leapMonth.Count == 5 && leapMonth[4].Start == leapMonth[4].End, "Leap day uses the fifth period");
var filteredWeeks = WeeklyCollectionPeriods.Create(new(2026, 10, 5), new(2026, 10, 16));
Check(filteredWeeks.Count == 3 && filteredWeeks[0].Start.Day == 5 && filteredWeeks[2].End.Day == 16,
    "Date filters clip weekly periods without changing month-based boundaries");
var days = fullMonth.SelectMany(week => Enumerable.Range(0, (week.End - week.Start).Days + 1).Select(offset => week.Start.AddDays(offset))).ToList();
Check(days.Count == 31 && days.Distinct().Count() == 31, "Weekly periods cover every month day exactly once");
Check(Access(UserRole.BURSER, 0).CanViewDashboard, "Bursar dashboard opens without any rights");
Check(Access(UserRole.BURSER, 1L).CanViewDashboard, "Retired dashboard bit has no effect on access");
Check(Access(UserRole.CLASS_TEACHER, 0).CanViewDashboard, "Teacher dashboard is role-based");
Check(Access(UserRole.ADMIN, 0).CanViewDashboard, "Administration dashboard is role-based");
Check(!Access(UserRole.PARENT).CanViewDashboard, "Unsupported roles do not get staff dashboards");
Check(!Access(UserRole.BURSER, 0, false).CanViewDashboard, "Anonymous users cannot get staff dashboards");
var teacher = Access(UserRole.CLASS_TEACHER, nominate: true);
var bursar = Access(UserRole.BURSER);
var admin = Access(UserRole.ADMIN);
var configuredTeacherRights = AccessRights.ViewStaff | AccessRights.ViewGrades | AccessRights.ViewClasses
    | AccessRights.ViewSubjects | AccessRights.ViewTerms | AccessRights.ViewTimetable | AccessRights.ViewLearners
    | AccessRights.MarkAttendance | AccessRights.GenerateReportCards | AccessRights.ViewHouses | AccessRights.ViewVillages
    | AccessRights.ViewAllBursarsCashUp | AccessRights.ViewSports | AccessRights.ViewPrefects;
var configuredTeacher = Access(UserRole.CLASS_TEACHER, (long)configuredTeacherRights, nominate: true);
Check(configuredTeacher.Navigation.Count() == 15, "Teacher navigation includes nomination tracking with the assigned tools");
Check(configuredTeacher.CanOpen("Academics", "/Staff/Index"), "Granted teacher staff viewing is available");
Check(configuredTeacher.CanOpen("Config", "/Grades/Index"), "Granted teacher grade viewing is available");
Check(configuredTeacher.CanOpen("Config", "/Villages/Index"), "Granted teacher village viewing is available");
Check(!Access(UserRole.CLASS_TEACHER, 0, nominate: true).CanOpen("Academics", "/Staff/Index"), "Nomination permission cannot grant staff viewing");
Check(!Access(UserRole.CLASS_TEACHER, (long)AccessRights.ViewAllBursarsCashUp).CanNominatePrefects,
    "Cash-up rights do not grant nomination permission");
Check(!Access(UserRole.BURSER, ~0L, nominate: true).CanNominatePrefects, "Bursar role cannot nominate even with broad rights");
var flagValues = Enum.GetValues<AccessRights>().Where(x => x != AccessRights.None).Select(x => (long)x).ToList();
Check(flagValues.Distinct().Count() == flagValues.Count, "All bitmask access rights have unique persisted values");
Check(!RoleAssignmentPolicy.CanAssign((int)UserRole.HEAD, (int)UserRole.ADMIN), "Head cannot grant the system administrator role");
Check(!RoleAssignmentPolicy.CanAssign((int)UserRole.DEPUTY_HEAD, (int)(UserRole.ADMIN | UserRole.BURSER)), "Combined role flags cannot bypass administrator assignment protection");
Check(RoleAssignmentPolicy.CanAssign((int)UserRole.ADMIN, (int)UserRole.ADMIN), "System administrators retain administrator assignment capability");
Check(!RoleAssignmentPolicy.IsDefined(128), "Undefined system roles are rejected");
Check(!Enum.GetNames<UserRole>().Any(x => x.Contains("PREFECT") || x.Contains("SDC")), "Prefects and SDC members have no login roles");
Check(!RightsSelectionPolicy.IsValid(-1L) && !RightsSelectionPolicy.IsValid(1L) && !RightsSelectionPolicy.IsValid(0L),
    "All-rights masks, the retired dashboard bit and None cannot be submitted as checkbox permissions");
Check(RightsSelectionPolicy.IsValid((long)AccessRights.ReversePayments), "The signed high-bit payment-reversal right remains valid");
Check(teacher.CanOpen("Academics", "/Students/Index"), "Teacher with learner right sees students");
Check(!teacher.CanOpen("Finance", "/Fees/Index"), "Teacher must not see finance even with all rights");
Check(!teacher.IsRelevantPage("Finance", "/Fees/RecordPayment"), "Teacher cannot open a finance action directly");
Check(!teacher.CanOpen("Config", "/Users/Index"), "Teacher cannot manage system users");
Check(bursar.CanOpen("Finance", "/Fees/Index"), "Bursar sees finance");
Check(!bursar.CanOpen("Academics", "/Students/Index"), "Bursar does not receive the teaching directory");
Check(!bursar.IsRelevantPage("Sports", "/Sporting/Assign"), "Bursar cannot open teaching sports actions directly");
Check(!bursar.IsRelevantPage("Config", "/Classes/Upsert"), "Bursar cannot configure teaching classes");
Check(admin.CanOpen("Finance", "/Fees/Index") && admin.CanOpen("Academics", "/Students/Index"), "Administration spans both workspaces");
Check(Access(UserRole.CLASS_TEACHER, 0).Navigation.Single().Title == "Staff notices", "Staff notices are available to staff without operational rights");
Check(Access(UserRole.BURSER, (long)AccessRights.ViewLearners).Navigation.Single().Title == "Staff notices", "Irrelevant rights do not expose academic modules");
Check(Access(UserRole.CLASS_TEACHER, (long)AccessRights.ViewLearners).Navigation.Select(x => x.Title).ToHashSet().SetEquals(["Students", "Staff notices"]), "Only granted modules and staff notices are visible");
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
await CheckFilter(admin, "Academics", "/Notices/Index", true);
await CheckFilter(teacher, "Academics", "/Notices/Index", false);
await CheckFilter(bursar, "Academics", "/Notices/Index", false);
await CheckFilter(bursar, "Academics", "/StaffNotices/Index", true);
await CheckFilter(teacher, "Academics", "/StaffNotices/Details", true);
await CheckFilter(admin, "Academics", "/StaffNotices/Index", true);
await CheckFilter(admin, "Academics", "/Sdc/Index", true);
await CheckFilter(teacher, "Academics", "/Sdc/Index", false);
await CheckFilter(bursar, "Academics", "/Sdc/Positions", false);
await CheckFilter(teacher, "Academics", "/StaffNotices/Details", false, false);
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
    var principalWithoutAccount = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
    var current = new CurrentUserService(principalWithoutAccount, db);
    Check(!current.HasRight(AccessRights.None) && !current.CanNominatePrefects, "Anonymous users receive no role-free or nomination permission");
    principalWithoutAccount.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "checks"));
    current = new CurrentUserService(principalWithoutAccount, db);
    Check(!current.HasRight(AccessRights.None) && !current.CanNominatePrefects,
        "A Microsoft session without a mapped active school account does not grant staff access");
    foreach (var role in new[] { UserRole.CLASS_TEACHER, UserRole.BURSER, UserRole.PARENT })
    {
        var user = new FakeUser((int)role, ~0L, true);
        var appointments = new SMS.Web.Areas.Academics.Pages.Prefects.UpsertModel(db, user, new WorkspaceAccess(user));
        Check(await appointments.OnGetAsync(null) is ForbidResult, $"{role} cannot open the prefect appointment form");
        Check(await appointments.OnPostAsync(null) is ForbidResult, $"{role} cannot create appointments with broad rights");
        Check(await appointments.OnPostAsync(Guid.NewGuid()) is ForbidResult, $"{role} cannot edit appointments with broad rights");
        var nominations = new SMS.Web.Areas.Academics.Pages.Prefects.NominationsModel(db, user, new WorkspaceAccess(user));
        Check(await nominations.OnPostReviewAsync(Guid.NewGuid(), "reject", "Reason") is ForbidResult,
            $"{role} cannot decide nominations");
    }
    var noPrefectRights = new FakeUser((int)UserRole.ADMIN, 0, true);
    Check(await new SMS.Web.Areas.Academics.Pages.Prefects.UpsertModel(db, noPrefectRights, new WorkspaceAccess(noPrefectRights))
        .OnPostAsync(null) is ForbidResult, "Administration role alone does not grant appointment rights");
    var createUser = new FakeUser((int)UserRole.ADMIN, (long)AccessRights.CreatePrefects, true);
    var tamperedForm = new SMS.Web.Areas.Academics.Pages.Prefects.UpsertModel(db, createUser, new WorkspaceAccess(createUser));
    tamperedForm.Input.Id = Guid.NewGuid();
    Check(await tamperedForm.OnPostAsync(null) is BadRequestResult, "A create form cannot edit an appointment through a posted ID");
    var request = new PrefectAppointments.Request(Guid.NewGuid(), (int)PrefectPostTitle.PREFECT, (int)PrefectStatus.ACTIVE,
        new(2026, 10, 11), new(2026, 12, 4), null);
    Check(PrefectAppointments.Validate(request) == null, "Valid appointment details accepted");
    Check(PrefectAppointments.Validate(request with { EndDate = null }) != null, "Missing end date rejected without a crash");
    Check(PrefectAppointments.Validate(request with { EndDate = new(2026, 10, 10) }) != null, "Reversed appointment dates rejected");
    Check(PrefectAppointments.Validate(request with { StatusId = 99 }) != null, "Unknown appointment status rejected");
    Check(PrefectLifecycle.EffectiveStatus(PrefectStatus.ACTIVE, new(2026, 10, 10), new(2026, 10, 11)) == PrefectStatus.COMPLETED,
        "Expired active appointments display as completed");
    Check(PrefectLifecycle.Label(PrefectStatus.ACTIVE, new(2026, 10, 12), new(2026, 12, 4), new(2026, 10, 11)) == "Upcoming",
        "Future appointments display as upcoming");
    var nominationEntity = db.Model.FindEntityType(typeof(SMS.Data.PrefectNomination))!;
    Check(nominationEntity.GetIndexes().Any(x => x.IsUnique && x.Properties.Any(p => p.Name == "PrefectId")),
        "An appointment can be linked to only one nomination");
    var teacherScope = PrefectAccess.EligibleStudents(db, teacher, Guid.NewGuid()).ToQueryString();
    Check(teacherScope.Contains("ClassTeacherAssignment") && teacherScope.Contains("AcademicYear") && teacherScope.Contains("UserId"),
        "Teacher nomination candidates are limited to current assigned classes");
    foreach (var role in new[] { UserRole.ADMIN, UserRole.HEAD, UserRole.DEPUTY_HEAD })
        Check(SdcAccess.CanManage(Access(role)), $"{role} can manage SDC records");
    foreach (var role in new[] { UserRole.CLASS_TEACHER, UserRole.BURSER, UserRole.PARENT })
    {
        var user = new FakeUser((int)role, ~0L, true);
        var sdcAccess = new WorkspaceAccess(user);
        var members = new SMS.Web.Areas.Academics.Pages.Sdc.MemberModel(db, user, sdcAccess);
        Check(await members.OnGetAsync(null) is ForbidResult && await members.OnPostAsync(null) is ForbidResult,
            $"{role} cannot read or save SDC memberships");
        var terms = new SMS.Web.Areas.Academics.Pages.Sdc.TermsModel(db, user, sdcAccess);
        Check(await terms.OnPostAsync(null) is ForbidResult, $"{role} cannot save SDC terms");
        var positions = new SMS.Web.Areas.Academics.Pages.Sdc.PositionsModel(db, user, sdcAccess);
        Check(await positions.OnPostAsync(null) is ForbidResult, $"{role} cannot save SDC positions");
    }
    Check(!SdcAccess.CanManage(Access(UserRole.ADMIN, ~0L, false)), "Anonymous users cannot manage SDC records");
    var invalidTerm = new SMS.Web.Areas.Academics.Pages.Sdc.TermsModel.TermInput
        { Name = "Term", StartDate = new(2026, 10, 11), EndDate = new(2026, 10, 10) };
    Check(!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(invalidTerm,
        new System.ComponentModel.DataAnnotations.ValidationContext(invalidTerm), [], true), "Reversed SDC term dates rejected");
    var sdcEntity = db.Model.FindEntityType(typeof(SMS.Data.SdcMember))!;
    Check(sdcEntity.GetForeignKeys().All(x => x.DeleteBehavior == DeleteBehavior.Restrict), "SDC membership history cannot be cascade deleted");
    Check(db.Model.FindEntityType(typeof(SMS.Data.SdcTerm))!.GetIndexes().Any(x => x.IsUnique), "SDC term names are unique");
    Check(db.Model.FindEntityType(typeof(SMS.Data.SdcPosition))!.GetIndexes().Any(x => x.IsUnique), "SDC position names are unique");
    foreach (var role in new[] { UserRole.ADMIN, UserRole.HEAD, UserRole.DEPUTY_HEAD })
        Check(SchoolNotices.CanManage(Access(role)), $"{role} can manage public notices");
    foreach (var role in new[] { UserRole.CLASS_TEACHER, UserRole.BURSER, UserRole.PARENT })
    {
        var user = new FakeUser((int)role, ~0L, true);
        var editor = new SMS.Web.Areas.Academics.Pages.Notices.UpsertModel(db, user, new WorkspaceAccess(user));
        Check(await editor.OnPostAsync(null, "publish") is ForbidResult, $"{role} cannot publish notices even with broad rights");
        var list = new SMS.Web.Areas.Academics.Pages.Notices.IndexModel(db, new WorkspaceAccess(user));
        Check(await list.OnPostWithdrawAsync(Guid.NewGuid()) is ForbidResult, $"{role} cannot withdraw a public notice");
    }
    Check(!SchoolNotices.CanManage(Access(UserRole.ADMIN, ~0L, false)), "Anonymous users cannot manage notices");
    var noticesSql = SchoolNotices.Published(db).ToQueryString();
    Check(noticesSql.Contains("[ChannelId] = 4") && noticesSql.Contains("[StatusId] = 1"),
        "Public notice query excludes drafts and all internal announcement channels");
    var staffNoticesSql = SchoolNotices.StaffPublished(db).ToQueryString();
    Check(staffNoticesSql.Contains("[ChannelId] = 5") && staffNoticesSql.Contains("[StatusId] = 1"), "Staff readers receive only published staff notices");
    foreach (var role in new[] { UserRole.ADMIN, UserRole.HEAD, UserRole.DEPUTY_HEAD, UserRole.CLASS_TEACHER, UserRole.BURSER })
        Check(SchoolNotices.CanReadStaff(Access(role, 0)), $"{role} can read staff notices without operational rights");
    Check(!SchoolNotices.CanReadStaff(Access(UserRole.CLASS_TEACHER, ~0L, false)), "Anonymous users cannot read staff notices");
    var parent = new FakeUser((int)UserRole.PARENT, ~0L, true);
    var staffDetails = new SMS.Web.Areas.Academics.Pages.StaffNotices.DetailsModel(db, new WorkspaceAccess(parent));
    Check(await staffDetails.OnGetAsync(Guid.NewGuid()) is ForbidResult, "Non-staff users cannot retrieve a staff notice by ID");
    Check(new SMS.Web.Areas.Academics.Pages.Notices.UpsertModel.NoticeInput().Audience == (int)AnnouncementChannel.STAFF_NOTICE,
        "New notices default to staff-only visibility");
    var input = new SMS.Web.Areas.Academics.Pages.Notices.UpsertModel.NoticeInput { Title = new string('X', 51), Body = "Notice" };
    Check(!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(input,
        new System.ComponentModel.DataAnnotations.ValidationContext(input), [], true), "Oversized title rejected before database writes");
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

sealed class FakeUser(int role, long rights, bool authenticated, bool nominate = false) : ICurrentUserService
{
    public string? Email => "check@example.test";
    public string? Name => "Check user";
    public string? AzureId => null;
    public bool IsAuthenticated => authenticated;
    public Guid? UserId => Guid.Empty;
    public int? RoleId => role;
    public Guid? GroupId => Guid.Empty;
    public long Rights => rights;
    public bool CanNominatePrefects => authenticated && (nominate || (role & (int)UserRole.ADMIN) != 0);
    public bool HasRight(AccessRights right) => (rights & (long)right) == (long)right;
    public Task<Guid> GetUserIdAsync() => Task.FromResult(Guid.Empty);
    public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult<Guid?>(Guid.Empty);
}


