using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SMS.Lib;
using SMS.Web.Services;
using System.Security.Claims;
using Xunit;

namespace SMS.Web.Tests;

public sealed class GalleryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "sms-gallery-tests-" + Guid.NewGuid().ToString("N"));
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
    private GalleryStore Store() => new(new Environment(root), new ConfigurationBuilder().Build(), NullLogger<GalleryStore>.Instance);
    private static FormFile Upload(byte[]? bytes = null) => new(new MemoryStream(bytes ?? Png), 0, (bytes ?? Png).Length, "Photo", "photo.png");

    [Fact]
    public async Task DraftsArePrivateUntilPublishedAndSurviveRestart()
    {
        var store = Store();
        var photo = await store.CreateAsync(new() { Caption = "Sports day", Album = "Sports" }, Upload(), null);
        Assert.Empty(await store.ListAsync(false));
        Assert.Null(await store.FindAsync(photo.Id, false));
        Assert.Null(await store.ImageAsync(photo.Id, false, false));
        Assert.NotNull(await store.ImageAsync(photo.Id, false, true));
        await store.UpdateAsync(photo.Id, p => p with { Published = true });
        var restarted = Store();
        Assert.Single(await restarted.ListAsync(false));
        Assert.Equal(Png, (await restarted.ImageAsync(photo.Id, false, false))!.Bytes);
    }

    [Fact]
    public async Task ArchiveHidesBothImageAndThumbnailAndRestoreKeepsPhoto()
    {
        var store = Store();
        var photo = await store.CreateAsync(new() { Caption = "An event", Published = true }, Upload(), Upload());
        await store.UpdateAsync(photo.Id, p => p with { Archived = true, Published = false });
        Assert.Empty(await store.ListAsync(false));
        Assert.Null(await store.ImageAsync(photo.Id, false, false));
        Assert.Null(await store.ImageAsync(photo.Id, true, false));
        Assert.Single(await store.ListAsync(true));
        await store.UpdateAsync(photo.Id, p => p with { Archived = false });
        Assert.Empty(await store.ListAsync(false));
        Assert.Equal(Png, (await store.ImageAsync(photo.Id, true, true))!.Bytes);
    }

    [Fact]
    public async Task InvalidFilesNeverCommitAPhoto()
    {
        var store = Store();
        await Assert.ThrowsAsync<GalleryValidationException>(() => store.CreateAsync(new(), Upload("<svg onload='alert(1)'>"u8.ToArray()), null));
        await Assert.ThrowsAsync<GalleryValidationException>(() => store.CreateAsync(new(), Upload(new byte[SchoolGallery.MaximumImageBytes + 1]), null));
        await Assert.ThrowsAsync<GalleryValidationException>(() => store.CreateAsync(new(), Upload(), Upload("invalid"u8.ToArray())));
        Assert.Empty(await store.ListAsync(true));
    }

    [Fact]
    public async Task ArchivedSeedIsNotRepublishedOnRestart()
    {
        var images = Path.Combine(root, "wwwroot", "Images");
        Directory.CreateDirectory(images);
        await File.WriteAllBytesAsync(Path.Combine(images, "front_view.png"), Png);
        var store = Store();
        var seed = Assert.Single(await store.ListAsync(false));
        await store.UpdateAsync(seed.Id, p => p with { Archived = true, Published = false });
        Assert.Empty(await Store().ListAsync(false));
        Assert.Single(await Store().ListAsync(true));
    }

    [Theory]
    [InlineData(false, UserRole.ADMIN, true, false)]
    [InlineData(true, UserRole.STAFF, true, false)]
    [InlineData(true, UserRole.HEAD, false, false)]
    [InlineData(true, UserRole.HEAD, true, true)]
    public async Task ImageEndpointChecksBothAuthenticationAndManagementRights(bool authenticated, UserRole role, bool right, bool allowed)
    {
        var store = Store();
        var photo = await store.CreateAsync(new(), Upload(), null);
        var actor = new Actor(role, right);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "Test" : null)) };
        var page = new SMS.Web.Pages.Gallery.PhotoModel(store, new WorkspaceAccess(actor)) { PageContext = new PageContext { HttpContext = context } };
        var result = await page.OnGetAsync(photo.Id, false, default);
        if (allowed)
        {
            Assert.IsType<FileContentResult>(result);
            Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        }
        else Assert.IsType<NotFoundResult>(result);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    private sealed class Environment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "GalleryTests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class Actor(UserRole role, bool right) : ICurrentUserService
    {
        public string? Email => null;
        public string? Name => null;
        public string? IdentitySubject => null;
        public bool HasFeatureRight(FeatureRights right) => IsAuthenticated && ((FeatureRightsCatalog.Legacy(Rights) & (long)right) == (long)right);
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.Empty;
        public int? RoleId => (int)role;
        public Guid? GroupId => null;
        public long Rights => right ? (long)AccessRights.SystemSettings : 0;
        public bool CanNominatePrefects => false;
        public bool HasRight(AccessRights requested) => requested == AccessRights.None || right;
        public Task<Guid> GetUserIdAsync() => Task.FromResult(Guid.Empty);
        public Task<Guid?> EnsureUserExistsAsync() => Task.FromResult<Guid?>(Guid.Empty);
    }
}
