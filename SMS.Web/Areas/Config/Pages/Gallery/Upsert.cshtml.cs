using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Config.Pages.Gallery;

[Authorize, RequestSizeLimit(4 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 4 * 1024 * 1024)]
public class UpsertModel(IGalleryStore gallery, WorkspaceAccess access, ICurrentUserService user,
    ILogger<UpsertModel> logger) : PageModel
{
    public sealed class PhotoInput
    {
        [Required, StringLength(200)] public string Caption { get; set; } = "";
        [Required, StringLength(80)] public string Album { get; set; } = "School life";
        [Range(0, 9999)] public int Order { get; set; }
        public bool Published { get; set; }
    }
    [BindProperty] public PhotoInput Input { get; set; } = new();
    [BindProperty] public IFormFile? Photo { get; set; }
    [BindProperty] public IFormFile? Thumbnail { get; set; }
    public bool IsEdit { get; private set; }
    public bool CanPublish => SchoolGallery.CanPublish(access);
    public Guid? PhotoId { get; private set; }
    public List<string> Albums { get; private set; } = [];

    private async Task LoadAlbums(CancellationToken ct) => Albums = (await gallery.ListAsync(true, ct))
        .Where(x => !x.Archived).Select(x => x.Album).Concat(["School life", "Sports", "Events"])
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (!(id.HasValue ? SchoolGallery.CanEdit(access) : SchoolGallery.CanCreate(access))) return Forbid();
        IsEdit = id.HasValue; PhotoId = id;
        await LoadAlbums(ct);
        if (id.HasValue)
        {
            var photo = await gallery.FindAsync(id.Value, true, ct);
            if (photo == null || photo.Archived) return NotFound();
            if (photo.Published && !CanPublish) return Forbid();
            Input = new() { Caption = photo.Caption, Album = photo.Album, Order = photo.Order, Published = photo.Published };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        if (!(id.HasValue ? SchoolGallery.CanEdit(access) : SchoolGallery.CanCreate(access)) || !user.UserId.HasValue) return Forbid();
        IsEdit = id.HasValue; PhotoId = id;
        await LoadAlbums(ct);
        var existing = id.HasValue ? await gallery.FindAsync(id.Value, true, ct) : null;
        if (id.HasValue && (existing == null || existing.Archived)) return NotFound();
        if ((Input.Published || existing?.Published == true) && !CanPublish) return Forbid();
        Input.Caption = Input.Caption?.Trim() ?? ""; Input.Album = Input.Album?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(Input.Caption)) ModelState.AddModelError("Input.Caption", "Add a caption describing this photo.");
        if (string.IsNullOrWhiteSpace(Input.Album)) ModelState.AddModelError("Input.Album", "Choose or enter an album name.");
        if (!id.HasValue && Photo == null) ModelState.AddModelError(nameof(Photo), "Choose a photo to upload.");
        if (!ModelState.IsValid) return Page();
        try
        {
            if (id.HasValue)
                await gallery.UpdateAsync(id.Value, photo => {
                    if (photo.Published && !CanPublish) throw new GalleryPermissionException();
                    return photo with { Caption = Input.Caption, Album = Input.Album,
                        Order = Input.Order, Published = Input.Published, UpdatedBy = user.UserId };
                }, ct);
            else
                await gallery.CreateAsync(new GalleryPhoto { Caption = Input.Caption, Album = Input.Album,
                    Order = Input.Order, Published = Input.Published, UpdatedBy = user.UserId }, Photo!, Thumbnail, ct);
            TempData["StatusMessage"] = Input.Published ? "Photo saved and published on the school website." : "Draft saved. Publish it when ready.";
            return RedirectToPage("./Index");
        }
        catch (GalleryPermissionException) { return Forbid(); }
        catch (GalleryValidationException ex) { ModelState.AddModelError(nameof(Photo), ex.Message); }
        catch (IOException ex) { logger.LogError(ex, "Could not save a school gallery photo"); ModelState.AddModelError("", "The photo could not be saved. Please try again."); }
        return Page();
    }
}
