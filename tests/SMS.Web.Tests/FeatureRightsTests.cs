using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
using Xunit;
using GroupEdit = SMS.Web.Areas.Config.Pages.UserGroups.UpsertModel;
using NoticeEdit = SMS.Web.Areas.Academics.Pages.Notices.UpsertModel;
using TripEdit = SMS.Web.Areas.Academics.Pages.Trips.UpsertModel;
using GalleryEdit = SMS.Web.Areas.Config.Pages.Gallery.UpsertModel;
using GalleryList = SMS.Web.Areas.Config.Pages.Gallery.IndexModel;

namespace SMS.Web.Tests;

public class FeatureRightsTests
{
    private static SMSDbContext Database() => new(new DbContextOptionsBuilder<SMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static T Page<T>(T page) where T : PageModel
    {
        var http = new DefaultHttpContext(); page.PageContext = new() { HttpContext = http };
        page.TempData = new TempDataDictionary(http, new TempData()); return page;
    }
    private sealed class TempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
    public sealed class Actor(FeatureRights features, UserRole role = UserRole.HEAD, AccessRights rights = (AccessRights)(-1L)) : ICurrentUserService
    {
        public string? Email => "permission@example.test";
        public string? Name => "Permission tester";
        public string? IdentitySubject => "test";
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public int? RoleId => (int)role;
        public Guid? GroupId => null;
        public long Rights => (long)rights;
        public bool CanNominatePrefects => false;
        public bool HasRight(AccessRights right) => (Rights & (long)right) == (long)right;
        public bool HasFeatureRight(FeatureRights right) => (features & right) == right;
        public Task<Guid> GetUserIdAsync() => Task.FromResult(UserId!.Value);
        public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult(UserId);
    }

    [Fact]
    public void EveryFeatureRightIsUniqueSelectableAndFitsTheSeparateMask()
    {
        var values = Enum.GetValues<FeatureRights>().Where(x => x != FeatureRights.None).Select(x => (long)x).ToArray();
        Assert.Equal(29, values.Length);
        Assert.Equal(values.Length, values.Distinct().Count());
        Assert.All(values, x => { Assert.True(x > 0 && (x & (x - 1)) == 0); Assert.True(FeatureRightsCatalog.IsValid(x)); });
        Assert.Equal(values.Order(), FeatureRightsCatalog.Categories.SelectMany(x => x.Rights).Select(x => (long)x.Right).Order());
        Assert.False(FeatureRightsCatalog.IsValid(0)); Assert.False(FeatureRightsCatalog.IsValid(-1));
        Assert.False(FeatureRightsCatalog.IsValid(1L << 40)); Assert.False(FeatureRightsCatalog.IsValid(3));
        Assert.Equal(0, new UserGroup().FeatureRightsId);
    }

    [Theory]
    [InlineData("Academics", "/Trips/Index", FeatureRights.ViewTrips)]
    [InlineData("Academics", "/Notices/Index", FeatureRights.ViewNotices)]
    [InlineData("Academics", "/StaffNotices/Index", FeatureRights.ViewStaffNotices)]
    [InlineData("Academics", "/Sdc/Index", FeatureRights.ViewSdcMembers)]
    [InlineData("Academics", "/Sdc/Positions", FeatureRights.ViewSdcPositions)]
    [InlineData("Academics", "/Sdc/Terms", FeatureRights.ViewSdcTerms)]
    [InlineData("Config", "/Gallery/Index", FeatureRights.ViewGallery)]
    [InlineData("Faith", "/Masses/Index", FeatureRights.ViewMasses)]
    [InlineData("Reports", "/Index", FeatureRights.ViewReports)]
    public void NavigationRequiresTheSpecificViewRight(string area, string page, FeatureRights right)
    {
        Assert.False(new WorkspaceAccess(new Actor(FeatureRights.None)).CanOpen(area, page));
        var access = new WorkspaceAccess(new Actor(right, rights: AccessRights.None));
        Assert.True(access.CanOpen(area, page));
        Assert.Contains(access.Navigation, x => x.Area == area && x.Page == page);
    }

    [Theory]
    [InlineData(UserRole.STAFF)] [InlineData(UserRole.BURSER)]
    public void GrantsDoNotBypassExistingRoleBoundaries(UserRole role)
    {
        var access = new WorkspaceAccess(new Actor((FeatureRights)FeatureRightsCatalog.All, role));
        Assert.False(SchoolTrips.CanCreate(access)); Assert.False(SchoolNotices.CanManage(access));
        Assert.False(SdcAccess.CanViewMembers(access)); Assert.False(SchoolGallery.CanManage(access));
        Assert.False(SchoolMasses.CanCreate(access));
    }

    private static CurrentUserService RealUser(SMSDbContext db, User user)
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("email", user.Email), new Claim("email_verified", "true")], "test")) };
        return new(new HttpContextAccessor { HttpContext = http }, db);
    }
    [Theory]
    [InlineData(null)] [InlineData(0L)] [InlineData((long)FeatureRights.ViewTrips)]
    public void ExistingNullGroupsKeepLegacyAccessButExplicitZeroRevokesIt(long? configured)
    {
        using var db = Database();
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Head", FeatureRightsId = configured, RightsId = (long)(AccessRights.SystemSettings | AccessRights.ViewTimetable) };
        var user = new User { Id = Guid.NewGuid(), Email = "head@example.test", LoginId = "head", RoleId = (int)UserRole.HEAD, IsActive = true, GroupId = group.Id };
        db.AddRange(group, user); db.SaveChanges();
        var service = RealUser(db, user);
        Assert.Equal(configured == null || configured == (long)FeatureRights.ViewTrips, service.HasFeatureRight(FeatureRights.ViewTrips));
        Assert.Equal(configured == null, service.HasFeatureRight(FeatureRights.ViewGallery));
        Assert.Equal(configured == null, service.HasFeatureRight(FeatureRights.ViewMasses));
        Assert.True(service.HasRight(AccessRights.SystemSettings));
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AdministratorOverrideRequiresAnActiveAccount(bool active)
    {
        using var db = Database();
        var user = new User { Id = Guid.NewGuid(), Email = "admin@example.test", LoginId = "admin", RoleId = (int)UserRole.ADMIN, IsActive = active };
        db.Add(user); db.SaveChanges();
        var service = RealUser(db, user);
        Assert.Equal(active, service.HasFeatureRight((FeatureRights)FeatureRightsCatalog.All));
    }
    [Fact]
    public void UngroupedUsersRetainTheirPreviousRoleScopedAccess()
    {
        using var db = Database();
        var user = new User { Id = Guid.NewGuid(), Email = "teacher@example.test", LoginId = "teacher", RoleId = (int)UserRole.STAFF, IsActive = true };
        db.Add(user); db.SaveChanges();
        var access = new WorkspaceAccess(RealUser(db, user));
        Assert.True(SchoolTrips.CanView(access)); Assert.False(SchoolTrips.CanCreate(access));
        Assert.True(SchoolNotices.CanReadStaff(access)); Assert.False(SchoolGallery.CanManage(access));
    }
    [Fact]
    public async Task GroupEditMakesLegacyRightsExplicitAndPreservesOldSignedBits()
    {
        using var db = Database(); var actor = new Actor(FeatureRights.None);
        var old = AccessRights.ReversePayments | AccessRights.ViewUserGroups | AccessRights.SystemSettings;
        db.Users.Add(new User { Id = actor.UserId!.Value, LoginId = "creator", Email = actor.Email!, Name = "Creator", IsActive = true });
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Existing", RightsId = (long)old, FeatureRightsId = null, CreatorId = actor.UserId!.Value };
        db.Add(group); await db.SaveChangesAsync();
        var page = Page(new GroupEdit(db, actor));
        Assert.IsType<PageResult>(await page.OnGetAsync(group.Id));
        Assert.True(page.UsesLegacyFeatureRights);
        Assert.Contains((long)FeatureRights.PublishGalleryPhotos, page.SelectedFeatureRights);
        page.SelectedFeatureRights = [(long)FeatureRights.ViewTrips];
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(group.Id));
        db.ChangeTracker.Clear(); var saved = await db.UserGroups.SingleAsync();
        Assert.Equal((long)old, saved.RightsId); Assert.Equal((long)FeatureRights.ViewTrips, saved.FeatureRightsId);
        var details = new SMS.Web.Areas.Config.Pages.UserGroups.DetailsModel(db);
        Assert.IsType<PageResult>(details.OnGet(saved.Id));
        Assert.Equal(4, details.TotalRightsGranted);
    }
    [Theory]
    [InlineData(-1L)] [InlineData(3L)] [InlineData(1L << 40)]
    public async Task InvalidFeatureSelectionsCannotBeSaved(long value)
    {
        using var db = Database(); var actor = new Actor(FeatureRights.None);
        var page = Page(new GroupEdit(db, actor) { UserGroupVM = new() { Name = "Invalid" }, SelectedFeatureRights = [value] });
        Assert.IsType<PageResult>(await page.OnPostAsync(null)); Assert.Empty(db.UserGroups); Assert.False(page.ModelState.IsValid);
    }

    [Fact]
    public async Task ViewOnlyCannotSubmitAnyManagementForm()
    {
        using var db = Database();
        var actor = new Actor(FeatureRights.ViewTrips | FeatureRights.ViewNotices | FeatureRights.ViewSdcMembers | FeatureRights.ViewSdcPositions | FeatureRights.ViewSdcTerms | FeatureRights.ViewGallery | FeatureRights.ViewMasses);
        var access = new WorkspaceAccess(actor); var id = Guid.NewGuid();
        foreach (var target in new Guid?[] { null, id })
        {
            Assert.IsType<ForbidResult>(await Page(new TripEdit(db, actor, access)).OnPostAsync(target, default));
            Assert.IsType<ForbidResult>(await Page(new NoticeEdit(db, actor, access)).OnPostAsync(target, "draft"));
            Assert.IsType<ForbidResult>(await Page(new SMS.Web.Areas.Academics.Pages.Sdc.MemberModel(db, actor, access)).OnPostAsync(target));
            Assert.IsType<ForbidResult>(await Page(new SMS.Web.Areas.Academics.Pages.Sdc.PositionsModel(db, actor, access)).OnPostAsync(target));
            Assert.IsType<ForbidResult>(await Page(new SMS.Web.Areas.Academics.Pages.Sdc.TermsModel(db, actor, access)).OnPostAsync(target));
            Assert.IsType<ForbidResult>(await Page(new SMS.Web.Areas.Faith.Pages.Masses.UpsertModel(db, access, actor)).OnPostAsync(target, default));
        }
        Assert.IsType<ForbidResult>(await new SMS.Web.Areas.Academics.Pages.Notices.IndexModel(db, access).OnPostWithdrawAsync(id));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task NoticeCreatorCanSaveDraftButCannotPublishEditOrWithdraw()
    {
        using var db = Database(); var actor = new Actor(FeatureRights.ViewNotices | FeatureRights.CreateNotices); var access = new WorkspaceAccess(actor);
        var page = Page(new NoticeEdit(db, actor, access) { Input = new() { Title = "Draft", Body = "Staff text", Audience = 5 } });
        Assert.IsType<ForbidResult>(await page.OnPostAsync(null, "publish")); Assert.Empty(db.Announcements);
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(null, "draft"));
        var notice = await db.Announcements.SingleAsync(); Assert.Equal((int)AnnouncementStatus.DRAFT, notice.StatusId);
        Assert.IsType<ForbidResult>(await page.OnPostAsync(notice.Id, "draft"));
        Assert.IsType<ForbidResult>(await new SMS.Web.Areas.Academics.Pages.Notices.IndexModel(db, access).OnPostWithdrawAsync(notice.Id));
    }
    [Fact]
    public async Task DraftEditorCannotAlterPublishedNoticesIncludingConcurrentPublication()
    {
        var options = new DbContextOptionsBuilder<SMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var editor = new SMSDbContext(options); await using var publisher = new SMSDbContext(options);
        var notice = new Announcement { Id = Guid.NewGuid(), Title = "Original", Body = "Original", ChannelId = 5, StatusId = (int)AnnouncementStatus.DRAFT };
        editor.Add(notice); await editor.SaveChangesAsync();
        var published = await publisher.Announcements.SingleAsync(); published.StatusId = (int)AnnouncementStatus.SENT; await publisher.SaveChangesAsync();
        var actor = new Actor(FeatureRights.ViewNotices | FeatureRights.EditNotices);
        var page = Page(new NoticeEdit(editor, actor, new(actor)) { Input = new() { Title = "Changed", Body = "Changed", Audience = 5 } });
        Assert.IsType<ConflictObjectResult>(await page.OnPostAsync(notice.Id, "draft"));
        publisher.ChangeTracker.Clear(); Assert.Equal("Original", (await publisher.Announcements.SingleAsync()).Body);
        editor.ChangeTracker.Clear();
        Assert.IsType<ForbidResult>(await page.OnGetAsync(notice.Id));
        Assert.IsType<ForbidResult>(await page.OnPostAsync(notice.Id, "draft"));
    }

    [Theory]
    [InlineData(FeatureRights.CreateTrips, true, false)] [InlineData(FeatureRights.EditTrips, false, true)]
    public void TripCreateAndEditAreIndependent(FeatureRights right, bool create, bool edit)
    {
        var access = new WorkspaceAccess(new Actor(FeatureRights.ViewTrips | right));
        Assert.Equal(create, SchoolTrips.CanCreate(access)); Assert.Equal(edit, SchoolTrips.CanEdit(access));
        Assert.True(SchoolTrips.CanViewDrafts(access));
        Assert.False(SchoolTrips.CanViewDrafts(new(new Actor(FeatureRights.ViewTrips))));
        Assert.True(SchoolTrips.CanViewDrafts(new(new Actor(FeatureRights.ViewTrips | FeatureRights.ViewTripDrafts))));
    }
    [Fact]
    public async Task SdcViewOnlyLoadsListsAndCreatorCanAddButNotEdit()
    {
        using var db = Database(); var viewer = new Actor(FeatureRights.ViewSdcPositions); var view = Page(new SMS.Web.Areas.Academics.Pages.Sdc.PositionsModel(db, viewer, new(viewer)));
        Assert.IsType<PageResult>(await view.OnGetAsync(null)); Assert.False(view.CanSave);
        Assert.IsType<ForbidResult>(await view.OnGetAsync(Guid.NewGuid()));
        var creator = new Actor(FeatureRights.ViewSdcPositions | FeatureRights.CreateSdcPositions);
        var page = Page(new SMS.Web.Areas.Academics.Pages.Sdc.PositionsModel(db, creator, new(creator)) { Input = new() { Name = "Secretary" } });
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync(null));
        Assert.IsType<ForbidResult>(await page.OnPostAsync((await db.SdcPositions.SingleAsync()).Id));
    }
    [Fact]
    public void MassRightsNoLongerRequireLegacyTimetableBits()
    {
        var access = new WorkspaceAccess(new Actor(FeatureRights.ViewMasses | FeatureRights.CreateMasses, rights: AccessRights.None));
        Assert.True(SchoolMasses.CanCreate(access)); Assert.False(SchoolMasses.CanEdit(access));
    }
    [Theory]
    [InlineData("learners", FeatureRights.None, AccessRights.ViewLearners, false)]
    [InlineData("learners", FeatureRights.ViewReports, AccessRights.None, false)]
    [InlineData("learners", FeatureRights.ViewReports, AccessRights.ViewLearners, true)]
    [InlineData("collections", FeatureRights.ViewReports, AccessRights.ViewFeeBalances, false)]
    [InlineData("trips", FeatureRights.ViewReports, AccessRights.None, false)]
    [InlineData("trips", FeatureRights.ViewReports | FeatureRights.ViewTrips, AccessRights.None, true)]
    [InlineData("masses", FeatureRights.ViewReports | FeatureRights.ViewMasses, AccessRights.None, true)]
    [InlineData("sdc", FeatureRights.ViewReports, AccessRights.None, false)]
    [InlineData("notices", FeatureRights.ViewReports | FeatureRights.ViewNotices, AccessRights.None, true)]
    public void ReportsRequireBothReportAndUnderlyingModuleAccess(string key, FeatureRights features, AccessRights rights, bool allowed)
        => Assert.Equal(allowed, ReportCatalog.Allowed(ReportCatalog.Find(key)!, new(new Actor(features, rights: rights))));

    [Theory]
    [InlineData(FeatureRights.None, false, false)]
    [InlineData(FeatureRights.PrintReports, true, false)]
    [InlineData(FeatureRights.ExportReports, false, true)]
    public async Task ReportPrintAndExportEndpointsRequireSeparateRights(FeatureRights extra, bool print, bool export)
    {
        using var db = Database(); var actor = new Actor(FeatureRights.ViewReports | extra, rights: AccessRights.ViewStaff); var access = new WorkspaceAccess(actor);
        var page = Page(new SMS.Web.Areas.Reports.Pages.RunModel(db, new(db, actor, access), access, actor));
        Assert.IsType<PageResult>(await page.OnGetAsync("staff", default));
        var printed = await page.OnGetPrintAsync("staff", default);
        var exported = await page.OnGetExportAsync("staff", default);
        Assert.Equal(print, printed is PageResult); Assert.Equal(!print, printed is ForbidResult);
        Assert.Equal(export, exported is FileContentResult); Assert.Equal(!export, exported is ForbidResult);
    }

    private sealed class Store : IGalleryStore
    {
        public GalleryPhoto Photo = new() { Id = Guid.NewGuid(), Caption = "Photo" };
        public bool RacePublish;
        public Task<GalleryPhoto?> FindAsync(Guid id, bool includeDrafts, CancellationToken ct = default) => Task.FromResult<GalleryPhoto?>(Photo);
        public Task<List<GalleryPhoto>> ListAsync(bool includeDrafts, CancellationToken ct = default) => Task.FromResult(new List<GalleryPhoto> { Photo });
        public Task<GalleryPhoto> CreateAsync(GalleryPhoto photo, IFormFile image, IFormFile? thumbnail, CancellationToken ct = default) { Photo = photo; return Task.FromResult(photo); }
        public Task<GalleryImage?> ImageAsync(Guid id, bool thumbnail, bool includeDrafts, CancellationToken ct = default) => Task.FromResult<GalleryImage?>(null);
        public Task<bool> UpdateAsync(Guid id, Func<GalleryPhoto, GalleryPhoto> update, CancellationToken ct = default)
        { if (RacePublish) Photo = Photo with { Published = true }; Photo = update(Photo); return Task.FromResult(true); }
    }
    [Theory]
    [InlineData("publish")] [InlineData("unpublish")] [InlineData("archive")] [InlineData("restore")]
    public async Task GalleryViewDoesNotAuthorizeStateChanges(string action)
    {
        var actor = new Actor(FeatureRights.ViewGallery); var store = new Store();
        var page = Page(new GalleryList(store, new(actor), actor, NullLogger<GalleryList>.Instance));
        Assert.IsType<PageResult>(await page.OnGetAsync(default));
        Assert.IsType<ForbidResult>(await page.OnPostStateAsync(store.Photo.Id, action, default));
        Assert.IsType<ForbidResult>(await new GalleryEdit(store, new(actor), actor, NullLogger<GalleryEdit>.Instance).OnPostAsync(null, default));
    }
    [Fact]
    public async Task GalleryDraftEditorCannotPublishOrOverwriteConcurrentPublication()
    {
        var actor = new Actor(FeatureRights.ViewGallery | FeatureRights.EditGalleryPhotos); var store = new Store();
        var page = Page(new GalleryEdit(store, new(actor), actor, NullLogger<GalleryEdit>.Instance) { Input = new() { Caption = "Changed", Published = true } });
        Assert.IsType<ForbidResult>(await page.OnPostAsync(store.Photo.Id, default));
        page.Input.Published = false; store.RacePublish = true;
        Assert.IsType<ForbidResult>(await page.OnPostAsync(store.Photo.Id, default));
        Assert.True(store.Photo.Published); Assert.Equal("Photo", store.Photo.Caption);
    }
    [Fact]
    public async Task GalleryArchiveCannotWithdrawConcurrentlyPublishedPhotoWithoutPublishRight()
    {
        var actor = new Actor(FeatureRights.ViewGallery | FeatureRights.ArchiveGalleryPhotos); var store = new Store { RacePublish = true };
        var page = Page(new GalleryList(store, new(actor), actor, NullLogger<GalleryList>.Instance));
        Assert.IsType<ForbidResult>(await page.OnPostStateAsync(store.Photo.Id, "archive", default));
        Assert.False(store.Photo.Archived); Assert.True(store.Photo.Published);
    }

    [Fact]
    public void MigrationOnlyAddsNullableFeatureMaskAndLeavesExistingAssignmentsUntouched()
    {
        using var db = new SMSDbContext(new DbContextOptionsBuilder<SMSDbContext>().UseSqlServer("Server=localhost;Database=offline;Integrated Security=true").Options);
        var assembly = db.GetService<IMigrationsAssembly>();
        var migrationType = assembly.Migrations.Single(x => x.Key.EndsWith("_FeatureAccessRights")).Value;
        var migration = assembly.CreateMigration(migrationType, db.Database.ProviderName!);
        var column = Assert.IsType<AddColumnOperation>(Assert.Single(migration.UpOperations));
        Assert.Equal("UserGroup", column.Table); Assert.Equal("FeatureRightsId", column.Name);
        Assert.True(column.IsNullable); Assert.Null(column.DefaultValue); Assert.Null(column.DefaultValueSql);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.True(db.Model.FindEntityType(typeof(Announcement))!.FindProperty(nameof(Announcement.StatusId))!.IsConcurrencyToken);
    }
}
