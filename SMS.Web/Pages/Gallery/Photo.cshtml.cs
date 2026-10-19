using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMS.Web.Services;

namespace SMS.Web.Pages.Gallery;

[AllowAnonymous]
public class PhotoModel(IGalleryStore gallery, WorkspaceAccess access) : PageModel
{
    public async Task<IActionResult> OnGetAsync(Guid id, bool thumbnail, CancellationToken ct)
    {
        var canSeeDrafts = User.Identity?.IsAuthenticated == true && SchoolGallery.CanManage(access);
        var image = await gallery.ImageAsync(id, thumbnail, canSeeDrafts, ct);
        if (image == null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Bytes, image.ContentType);
    }
}
