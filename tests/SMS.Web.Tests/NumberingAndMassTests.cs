using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Pages.GeneratedNumbers;
using SMS.Web.Services;
using Xunit;
using MassEdit = SMS.Web.Areas.Faith.Pages.Masses.UpsertModel;
using MassDetails = SMS.Web.Areas.Faith.Pages.Masses.DetailsModel;

namespace SMS.Web.Tests;

public class NumberingAndMassTests
{
    private static SMSDbContext Database() => new(new DbContextOptionsBuilder<SMSDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    public static TheoryData<Type> NumberedTypes => new() { typeof(UserGroup), typeof(User), typeof(Subject), typeof(Class),
        typeof(Grade), typeof(House), typeof(Village), typeof(Term), typeof(Sport), typeof(Student), typeof(FeesStructure) };

    [Theory, MemberData(nameof(NumberedTypes))]
    public void AllNumberedTypesBreakTimestampTiesAndKeepPageAndDetailsConsistent(Type type)
    {
        using var db = Database();
        var rows = Array.CreateInstance(type, 3);
        for (var i = 0; i < 3; i++)
        {
            var row = Activator.CreateInstance(type)!;
            type.GetProperty("Id")!.SetValue(row, Guid.Parse($"00000000-0000-0000-0000-{i + 1:000000000000}"));
            type.GetProperty("CreationDate")!.SetValue(row, new DateTime(2026, 10, 18));
            foreach (var property in db.Model.FindEntityType(type)!.GetProperties().Where(x => !x.IsNullable && x.ClrType == typeof(string)))
                type.GetProperty(property.Name)!.SetValue(row, $"test-{i}");
            db.Add(row); rows.SetValue(row, i);
        }
        db.SaveChanges();
        var helper = typeof(GeneratedNumberHelper);
        helper.GetMethod($"Apply{type.Name}Numbers")!.Invoke(null, [db, rows]);
        var sequence = type.GetProperty(type.Name + "Sequence")!;
        Assert.Equal(new[] { 1, 2, 3 }, rows.Cast<object>().Select(x => (int)sequence.GetValue(x)!).OrderBy(x => x));
        var last = rows.GetValue(2)!;
        var expected = sequence.GetValue(last);
        sequence.SetValue(last, 0);
        var page = Array.CreateInstance(type, 1); page.SetValue(last, 0);
        helper.GetMethod($"Apply{type.Name}Numbers")!.Invoke(null, [db, page]);
        Assert.Equal(expected, sequence.GetValue(last));
        sequence.SetValue(last, 0);
        helper.GetMethod($"Apply{type.Name}Number")!.Invoke(null, [db, last]);
        Assert.Equal(expected, sequence.GetValue(last));
    }

    [Fact]
    public void LegacyNullCreationDateStillGetsAUniquePositiveNumber()
    {
        using var db = Database();
        var students = new[] { new Student { Id = Guid.NewGuid(), Name = "One", Surname = "Student", CreationDate = null },
            new Student { Id = Guid.NewGuid(), Name = "Two", Surname = "Student", CreationDate = null } };
        db.Students.AddRange(students); db.SaveChanges(); db.ApplyStudentNumbers(students);
        Assert.Equal(new[] { 1, 2 }, students.Select(x => x.StudentSequence).OrderBy(x => x));
    }

    [Fact]
    public async Task TeacherCanReadMassesButCannotCreateOrEditThem()
    {
        using var db = Database();
        var teacher = new Actor(UserRole.STAFF, AccessRights.ViewTimetable | AccessRights.CreateTimetable | AccessRights.EditTimetable);
        var access = new WorkspaceAccess(teacher);
        Assert.True(SchoolMasses.CanView(access));
        Assert.False(SchoolMasses.CanCreate(access)); Assert.False(SchoolMasses.CanEdit(access));
        Assert.True(access.IsRelevantPage("Faith", "/Masses/Index"));
        Assert.IsType<ForbidResult>(await new MassEdit(db, access, teacher).OnPostAsync(null, default));
        Assert.IsType<ForbidResult>(await new MassDetails(db, access, teacher).OnPostRemoveAsync(Guid.NewGuid(), Guid.NewGuid(), default));
    }

    [Fact]
    public async Task CreationAndEditingUseSeparateRightsAndPersistTheTimetable()
    {
        using var db = Database();
        var actor = new Actor(UserRole.HEAD, AccessRights.ViewTimetable | AccessRights.CreateTimetable);
        var page = new MassEdit(db, new WorkspaceAccess(actor), actor) { Input = new() {
            DateTime = new DateTime(2026, 10, 25, 8, 30, 0), LiturgicalDate = new DateOnly(2026, 10, 25), Venue = "School chapel", Celebrant = "Fr Example" } };
        Initialize(page);
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(null, default));
        var saved = Assert.Single(await db.MassEvents.ToListAsync());
        Assert.Equal(8, saved.DateTime.Hour); Assert.Equal(actor.UserId, saved.CreatorId);
        Assert.IsType<ForbidResult>(await page.OnPostAsync(saved.Id, default));
        var editor = new Actor(UserRole.HEAD, AccessRights.ViewTimetable | AccessRights.EditTimetable);
        var edit = new MassEdit(db, new WorkspaceAccess(editor), editor);
        Initialize(edit);
        Assert.IsType<PageResult>(await edit.OnGetAsync(saved.Id, default));
        edit.Input.Venue = "Main hall";
        Assert.IsType<RedirectToPageResult>(await edit.OnPostAsync(saved.Id, default));
        Assert.Equal("Main hall", (await db.MassEvents.FindAsync(saved.Id))!.Venue);
        Assert.Equal(actor.UserId, saved.CreatorId);
    }

    [Fact]
    public async Task InvalidScheduleAndRosterDoNotWriteAndRemovalIsScopedToMass()
    {
        using var db = Database();
        var actor = new Actor(UserRole.ADMIN, AccessRights.ViewTimetable | AccessRights.EditTimetable | AccessRights.CreateTimetable);
        var access = new WorkspaceAccess(actor);
        var empty = new MassEdit(db, access, actor); Initialize(empty);
        Assert.IsType<PageResult>(await empty.OnPostAsync(null, default));
        Assert.Empty(await db.MassEvents.ToListAsync());
        var mass = new MassEvent { Id = Guid.NewGuid(), Venue = "Chapel", Celebrant = "Fr Example", DateTime = DateTime.Today, LiturgicalDate = DateOnly.FromDateTime(DateTime.Today) };
        db.MassEvents.Add(mass); await db.SaveChangesAsync();
        var details = new MassDetails(db, access, actor) { Input = new() { Participant = "student:" + Guid.NewGuid(), RoleId = 999 } };
        Initialize(details);
        Assert.IsType<PageResult>(await details.OnPostAddAsync(mass.Id, default));
        Assert.Empty(await db.MassRosters.ToListAsync());
        var roster = new MassRoster { Id = Guid.NewGuid(), MassId = mass.Id, RoleId = (int)MinistryRole.READER, CreatorId = actor.UserId!.Value };
        db.MassRosters.Add(roster); await db.SaveChangesAsync();
        Assert.IsType<NotFoundResult>(await details.OnPostRemoveAsync(Guid.NewGuid(), roster.Id, default));
        Assert.Single(await db.MassRosters.ToListAsync());
    }

    [Fact]
    public async Task MinistryRosterCanBeAssignedWithoutDuplicatesAndRemoved()
    {
        using var db = Database();
        var actor = new Actor(UserRole.HEAD, AccessRights.ViewTimetable | AccessRights.EditTimetable);
        var access = new WorkspaceAccess(actor);
        var student = new Student { Id = Guid.NewGuid(), Name = "One", Surname = "Student", EnrolmentStatusId = (int)EnrolmentStatus.ACTIVE };
        var mass = new MassEvent { Id = Guid.NewGuid(), Venue = "Chapel", Celebrant = "Fr Example", DateTime = DateTime.Today, LiturgicalDate = DateOnly.FromDateTime(DateTime.Today) };
        db.Students.Add(student); db.MassEvents.Add(mass); await db.SaveChangesAsync();
        MassDetails Page()
        {
            var page = new MassDetails(db, access, actor) { Input = new() { Participant = "student:" + student.Id, RoleId = (int)MinistryRole.READER } };
            Initialize(page); return page;
        }
        Assert.IsType<RedirectToPageResult>(await Page().OnPostAddAsync(mass.Id, default));
        var saved = Assert.Single(await db.MassRosters.ToListAsync());
        Assert.Equal(student.Id, saved.StudentId); Assert.Null(saved.StaffId);
        Assert.IsType<PageResult>(await Page().OnPostAddAsync(mass.Id, default));
        Assert.Single(await db.MassRosters.ToListAsync());
        Assert.IsType<RedirectToPageResult>(await Page().OnPostRemoveAsync(mass.Id, saved.Id, default));
        Assert.Empty(await db.MassRosters.ToListAsync());
    }

    private static void Initialize(PageModel page)
    {
        var context = new DefaultHttpContext();
        page.PageContext = new PageContext { HttpContext = context };
        page.TempData = new TempDataDictionary(context, new TempData());
    }
    private sealed class TempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
    private sealed class Actor(UserRole role, AccessRights rights) : ICurrentUserService
    {
        public string? Email => null;
        public string? Name => null;
        public string? IdentitySubject => null;
        public bool HasFeatureRight(FeatureRights right) => IsAuthenticated && ((FeatureRightsCatalog.Legacy(Rights) & (long)right) == (long)right);
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public int? RoleId => (int)role;
        public Guid? GroupId => null;
        public long Rights => (long)rights;
        public bool CanNominatePrefects => false;
        public bool HasRight(AccessRights requested) => (rights & requested) == requested;
        public Task<Guid> GetUserIdAsync() => Task.FromResult(UserId!.Value);
        public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult(UserId);
    }
}
