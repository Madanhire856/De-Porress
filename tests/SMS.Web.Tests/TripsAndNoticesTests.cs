using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
using Xunit;
using TripEdit = SMS.Web.Areas.Academics.Pages.Trips.UpsertModel;
using NoticeEdit = SMS.Web.Areas.Academics.Pages.Notices.UpsertModel;

namespace SMS.Web.Tests;

public class TripsAndNoticesTests
{
    private static readonly Guid LeaderId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid GradeOne = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid GradeTwo = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static SMSDbContext Database()
    {
        var db = new SMSDbContext(new DbContextOptionsBuilder<SMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Staff.Add(new() { Id = LeaderId, Name = "Example", Surname = "Teacher", IdNumber = "TEST", IsActive = true });
        db.Grades.AddRange(new Grade { Id = GradeOne, Name = "Grade 5" }, new Grade { Id = GradeTwo, Name = "Grade 6" });
        db.Currencies.Add(new() { Code = "USD", Name = "US Dollar", Symbol = "$" });
        db.SaveChanges(); return db;
    }
    private static TripEdit.TripInput ValidTrip() => new() {
        Name = "Museum visit", Destination = "Museum", Purpose = "History learning", PlacesToPassThrough = "Town centre\nMuseum entrance",
        LeaderStaffId = LeaderId, AmountPerStudent = 25, CurrencyCode = "USD", PaymentDueDate = new DateOnly(2027, 1, 9),
        DepartureAt = new DateTime(2027, 1, 10, 8, 0, 0), ReturnAt = new DateTime(2027, 1, 11, 16, 0, 0),
        GradeIds = [GradeOne, GradeTwo], StatusId = (int)TripStatus.Planned, Capacity = 40, ParentConsentRequired = true
    };
    private static T Initialize<T>(T page) where T : PageModel
    {
        var context = new DefaultHttpContext(); page.PageContext = new PageContext { HttpContext = context };
        page.TempData = new TempDataDictionary(context, new TempData()); return page;
    }

    [Fact]
    public async Task TripSavesMultipleGradesRouteDurationAndCostWithoutCreatingPayments()
    {
        using var db = Database(); var actor = new Actor(UserRole.ADMIN);
        var page = Initialize(new TripEdit(db, actor, new(actor)) { Input = ValidTrip() });
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(null, default));
        var saved = Assert.Single(await db.SchoolTrips.Include(x => x.Grades).ToListAsync());
        Assert.Equal(2, saved.Grades.Count); Assert.Equal(2, saved.Days); Assert.Equal(1, saved.Nights);
        Assert.Equal(LeaderId, saved.LeaderStaffId); Assert.Equal(25, saved.AmountPerStudent);
        Assert.Equal("Town centre\nMuseum entrance", saved.PlacesToPassThrough); Assert.Equal(actor.UserId, saved.CreatorId);
        Assert.Empty(db.Payments); Assert.Empty(db.StudentLedgers);
    }

    [Theory]
    [InlineData("return")][InlineData("missingDate")][InlineData("negative")][InlineData("precision")]
    [InlineData("dueMissing")][InlineData("dueLate")][InlineData("leader")][InlineData("inactiveLeader")]
    [InlineData("noGrades")][InlineData("badGrade")][InlineData("currency")][InlineData("status")]
    [InlineData("capacity")][InlineData("contact")][InlineData("blankTitle")]
    public async Task InvalidTripDoesNotSave(string invalid)
    {
        using var db = Database(); var actor = new Actor(UserRole.ADMIN); var input = ValidTrip();
        switch (invalid)
        {
            case "return": input.ReturnAt = input.DepartureAt; break;
            case "missingDate": input.DepartureAt = null; break;
            case "negative": input.AmountPerStudent = -1; break;
            case "precision": input.AmountPerStudent = 1.111m; break;
            case "dueMissing": input.PaymentDueDate = null; break;
            case "dueLate": input.PaymentDueDate = new DateOnly(2027, 1, 12); break;
            case "leader": input.LeaderStaffId = Guid.NewGuid(); break;
            case "inactiveLeader": (await db.Staff.FindAsync(LeaderId))!.IsActive = false; await db.SaveChangesAsync(); break;
            case "noGrades": input.GradeIds.Clear(); break;
            case "badGrade": input.GradeIds = [Guid.NewGuid()]; break;
            case "currency": input.CurrencyCode = "BAD"; break;
            case "status": input.StatusId = 999; break;
            case "capacity": input.Capacity = 0; break;
            case "contact": input.EmergencyContactName = "Contact without phone"; break;
            case "blankTitle": input.Name = "   "; break;
        }
        var page = Initialize(new TripEdit(db, actor, new(actor)) { Input = input });
        Assert.IsType<PageResult>(await page.OnPostAsync(null, default));
        Assert.False(page.ModelState.IsValid); Assert.Empty(db.SchoolTrips); Assert.Empty(db.SchoolTripGrades);
    }

    [Fact]
    public async Task FreeDayTripNeedsNoPaymentDateAndEditingPreservesCreatorAndChangesGrades()
    {
        using var db = Database(); var creator = new Actor(UserRole.ADMIN);
        var create = Initialize(new TripEdit(db, creator, new(creator)) { Input = ValidTrip() });
        create.Input.AmountPerStudent = 0; create.Input.PaymentDueDate = null; create.Input.ReturnAt = create.Input.DepartureAt!.Value.AddHours(7);
        Assert.IsType<RedirectToPageResult>(await create.OnPostAsync(null, default));
        var saved = db.SchoolTrips.Single(); Assert.Equal(1, saved.Days); Assert.Equal(0, saved.Nights);
        var editor = new Actor(UserRole.HEAD); var edit = Initialize(new TripEdit(db, editor, new(editor)));
        Assert.IsType<PageResult>(await edit.OnGetAsync(saved.Id, default)); edit.Input.GradeIds = [GradeTwo];
        Assert.IsType<RedirectToPageResult>(await edit.OnPostAsync(saved.Id, default));
        Assert.Equal(creator.UserId, saved.CreatorId); Assert.Equal(editor.UserId, saved.UpdatedById);
        Assert.Equal(GradeTwo, Assert.Single(await db.SchoolTripGrades.ToListAsync()).GradeId);
    }

    [Fact]
    public async Task StaleEditDoesNotOverwriteAnotherAdministratorsChanges()
    {
        using var db = Database(); var actor = new Actor(UserRole.ADMIN);
        var create = Initialize(new TripEdit(db, actor, new(actor)) { Input = ValidTrip() }); await create.OnPostAsync(null, default);
        var saved = db.SchoolTrips.Single(); var edit = Initialize(new TripEdit(db, actor, new(actor))); await edit.OnGetAsync(saved.Id, default);
        saved.Name = "Other administrator's update"; saved.UpdatedAt = saved.UpdatedAt.AddMinutes(1); await db.SaveChangesAsync();
        edit.Input.Name = "Stale title";
        Assert.IsType<PageResult>(await edit.OnPostAsync(saved.Id, default));
        Assert.Equal("Other administrator's update", saved.Name);
    }

    [Theory][InlineData(UserRole.STAFF)][InlineData(UserRole.BURSER)]
    public async Task StaffCanReadPublishedTripPlansButCannotWriteOrReadDrafts(UserRole role)
    {
        using var db = Database(); var admin = new Actor(UserRole.ADMIN);
        var create = Initialize(new TripEdit(db, admin, new(admin)) { Input = ValidTrip() }); create.Input.StatusId = 0; await create.OnPostAsync(null, default);
        var trip = db.SchoolTrips.Single(); var actor = new Actor(role); var access = new WorkspaceAccess(actor);
        Assert.True(SchoolTrips.CanView(access)); Assert.False(SchoolTrips.CanManage(access));
        Assert.IsType<ForbidResult>(await Initialize(new TripEdit(db, actor, access)).OnPostAsync(trip.Id, default));
        Assert.IsType<NotFoundResult>(await new SMS.Web.Areas.Academics.Pages.Trips.DetailsModel(db, access).OnGetAsync(trip.Id, default));
        trip.StatusId = (int)TripStatus.Planned; await db.SaveChangesAsync();
        Assert.IsType<PageResult>(await new SMS.Web.Areas.Academics.Pages.Trips.DetailsModel(db, access).OnGetAsync(trip.Id, default));
    }

    [Theory][InlineData(4)][InlineData(5)]
    public async Task NoticeCreateAndEditRetainChosenAudience(int audience)
    {
        using var db = Database(); var actor = new Actor(UserRole.ADMIN);
        var page = Initialize(new NoticeEdit(db, actor, new(actor)) { Input = new() { Title = "Notice", Body = "Text", Audience = audience } });
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(null, "publish"));
        var notice = db.Announcements.Single(); Assert.Equal(audience, notice.ChannelId);
        var edit = Initialize(new NoticeEdit(db, actor, new(actor))); await edit.OnGetAsync(notice.Id);
        Assert.Equal(audience, edit.Input.Audience); edit.Input.Body = "Updated text";
        Assert.IsType<RedirectToPageResult>(await edit.OnPostAsync(notice.Id, "publish")); Assert.Equal(audience, notice.ChannelId);
        Assert.Equal(audience == 4 ? 1 : 0, SchoolNotices.Published(db).Count());
        Assert.Equal(audience == 5 ? 1 : 0, SchoolNotices.StaffPublished(db).Count());
        Assert.IsType<RedirectToPageResult>(await edit.OnPostAsync(notice.Id, "draft"));
        Assert.Empty(SchoolNotices.Published(db)); Assert.Empty(SchoolNotices.StaffPublished(db));
    }

    [Fact]
    public void StaffNoticePageExposesManagementOnlyToAdministrators()
    {
        using var db = Database(); var admin = new Actor(UserRole.ADMIN); var teacher = new Actor(UserRole.STAFF);
        Assert.True(new SMS.Web.Areas.Academics.Pages.StaffNotices.IndexModel(db, new(admin)).CanManage);
        Assert.False(new SMS.Web.Areas.Academics.Pages.StaffNotices.IndexModel(db, new(teacher)).CanManage);
        Assert.Contains(new WorkspaceAccess(admin).Navigation, x => x.Page == "/Notices/Index" && x.Group == "Notices");
    }

    private sealed class TempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
    private sealed class Actor(UserRole role) : ICurrentUserService
    {
        public string? Email => "admin@example.test";
        public string? Name => "Test actor";
        public string? IdentitySubject => "test";
        public bool HasFeatureRight(FeatureRights right) => IsAuthenticated && ((FeatureRightsCatalog.Legacy(Rights) & (long)right) == (long)right);
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public int? RoleId => (int)role;
        public Guid? GroupId => null;
        public long Rights => -1;
        public bool CanNominatePrefects => false;
        public bool HasRight(AccessRights right) => true;
        public Task<Guid> GetUserIdAsync() => Task.FromResult(UserId!.Value);
        public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult(UserId);
    }
}
