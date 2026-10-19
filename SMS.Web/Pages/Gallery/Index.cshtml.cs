using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SMS.Web.Services;

namespace SMS.Web.Pages.Gallery;

[AllowAnonymous]
public class IndexModel(IGalleryStore gallery) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Album { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<string> Albums { get; private set; } = [];
    public List<GalleryPhoto> Photos { get; private set; } = [];
    public bool HasNext { get; private set; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        var photos = await gallery.ListAsync(false, ct);
        Albums = photos.Select(x => x.Album).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var selected = string.IsNullOrWhiteSpace(Album) ? photos : photos.Where(x => string.Equals(x.Album, Album, StringComparison.OrdinalIgnoreCase)).ToList();
        PageNumber = Math.Clamp(PageNumber, 1, Math.Max(1, (selected.Count + 23) / 24));
        Photos = selected.Skip((PageNumber - 1) * 24).Take(24).ToList();
        HasNext = PageNumber * 24 < selected.Count;
    }
}
