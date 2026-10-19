using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Config.Pages.Gallery;

[Authorize]
public class IndexModel(IGalleryStore gallery, WorkspaceAccess access, ICurrentUserService user,
    ILogger<IndexModel> logger) : PageModel
{
    public bool CanCreate => SchoolGallery.CanCreate(access);
    public bool CanEdit => SchoolGallery.CanEdit(access);
    public bool CanPublish => SchoolGallery.CanPublish(access);
    public bool CanArchive => SchoolGallery.CanArchive(access);
    public List<GalleryPhoto> Photos { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public bool Archived { get; set; }
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? Album { get; set; }
    public List<string> Albums { get; private set; } = [];
    [TempData] public string? StatusMessage { get; set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!SchoolGallery.CanManage(access)) return Forbid();
        Photos = (await gallery.ListAsync(true, ct)).Where(x => x.Archived == Archived).ToList();
        Albums = Photos.Select(x => x.Album).Distinct().OrderBy(x => x).ToList();
        if (!string.IsNullOrEmpty(Album)) Photos = Photos.Where(x => x.Album == Album).ToList();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) Photos = Photos.Where(x => x.Caption.Contains(SearchTerm.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        return Page();
    }
    public async Task<IActionResult> OnPostStateAsync(Guid id, string action, CancellationToken ct)
    {
        if (!SchoolGallery.CanManage(access) || !user.UserId.HasValue) return Forbid();
        if (action is not ("publish" or "unpublish" or "archive" or "restore")) return BadRequest();
        if (action is "publish" or "unpublish" ? !CanPublish : !CanArchive) return Forbid();
        var current = await gallery.FindAsync(id, true, ct);
        if (current == null) return NotFound();
        if (action == "archive" && current.Published && !CanPublish) return Forbid();
        try
        {
            var found = await gallery.UpdateAsync(id, photo => {
                if (action == "archive" && photo.Published && !CanPublish) throw new GalleryPermissionException();
                return action switch {
                "publish" when !photo.Archived => photo with { Published = true, UpdatedBy = user.UserId },
                "unpublish" => photo with { Published = false, UpdatedBy = user.UserId },
                "archive" => photo with { Published = false, Archived = true, UpdatedBy = user.UserId },
                "restore" => photo with { Published = false, Archived = false, UpdatedBy = user.UserId },
                _ => photo
                };
            }, ct);
            if (!found) return NotFound();
            StatusMessage = action switch {
                "publish" => "Photo published on the school website.", "unpublish" => "Photo is now a draft and hidden from the public gallery.",
                "archive" => "Photo archived and removed from the public gallery.", _ => "Photo restored as a draft. Publish it when ready." };
        }
        catch (GalleryPermissionException) { return Forbid(); }
        catch (IOException ex) { logger.LogError(ex, "Could not change gallery photo state {PhotoId}", id); StatusMessage = "The photo could not be updated. Please try again."; }
        return RedirectToPage(new { Archived = action == "restore" || Archived, SearchTerm, Album });
    }
}
