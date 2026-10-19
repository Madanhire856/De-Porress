using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;
namespace SMS.Web.Areas.Academics.Pages.Sdc;
[Authorize]
public class PositionsModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty] public PositionInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    public List<SdcPosition> Positions { get; private set; } = [];
    public bool IsEdit { get; private set; }
    public bool CanCreate => SdcAccess.CanCreatePositions(access);
    public bool CanEdit => SdcAccess.CanEditPositions(access);
    public bool CanSave => IsEdit ? CanEdit : CanCreate;
    public bool CanViewMembers => SdcAccess.CanViewMembers(access);
    public class PositionInput
    {
        [Required, StringLength(80)] public string Name { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!SdcAccess.CanViewPositions(access) || (id.HasValue && !CanEdit)) return Forbid();
        IsEdit = id.HasValue;
        if (id.HasValue)
        {
            var position = await db.SdcPositions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (position == null) return NotFound();
            Input = new() { Name = position.Name, IsActive = position.IsActive };
        }
        await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (!(id.HasValue ? CanEdit : CanCreate) || !user.UserId.HasValue) return Forbid();
        IsEdit = id.HasValue;
        var position = id.HasValue ? await db.SdcPositions.SingleOrDefaultAsync(x => x.Id == id) : null;
        if (id.HasValue && position == null) return NotFound();
        var name = (Input.Name ?? "").Trim();
        if (name.Length == 0) ModelState.AddModelError("Input.Name", "Enter a position name.");
        if (await db.SdcPositions.AnyAsync(x => x.Name == name && x.Id != (id ?? Guid.Empty))) ModelState.AddModelError("Input.Name", "This position already exists.");
        await LoadAsync(); if (!ModelState.IsValid) return Page();
        if (position == null) { position = new() { Id = Guid.NewGuid(), CreatorId = user.UserId.Value, CreationDate = DateTime.UtcNow }; db.SdcPositions.Add(position); }
        position.Name = name; position.IsActive = Input.IsActive;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "The position could not be saved. Check that its name is unique."); return Page(); }
        TempData["SdcSaved"] = "SDC position saved."; return RedirectToPage("./Positions");
    }
    private async Task LoadAsync()
    {
        var query = db.SdcPositions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) query = query.Where(x => x.Name.Contains(SearchTerm));
        if (Status == "active") query = query.Where(x => x.IsActive);
        if (Status == "inactive") query = query.Where(x => !x.IsActive);
        Positions = await query.OrderBy(x => x.Name).ToListAsync();
    }
}
