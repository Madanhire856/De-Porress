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
public class TermsModel(SMSDbContext db, ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    [BindProperty] public TermInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    public List<SdcTerm> Terms { get; private set; } = [];
    public bool IsEdit { get; private set; }
    public bool CanCreate => SdcAccess.CanCreateTerms(access);
    public bool CanEdit => SdcAccess.CanEditTerms(access);
    public bool CanSave => IsEdit ? CanEdit : CanCreate;
    public bool CanViewMembers => SdcAccess.CanViewMembers(access);
    public class TermInput : IValidatableObject
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required, DataType(DataType.Date)] public DateTime? StartDate { get; set; }
        [Required, DataType(DataType.Date)] public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
                yield return new ValidationResult("End date must be on or after the start date.", [nameof(EndDate)]);
        }
    }
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!SdcAccess.CanViewTerms(access) || (id.HasValue && !CanEdit)) return Forbid();
        IsEdit = id.HasValue;
        if (id.HasValue)
        {
            var term = await db.SdcTerms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (term == null) return NotFound();
            Input = new() { Name = term.Name, StartDate = term.StartDate, EndDate = term.EndDate, IsActive = term.IsActive };
        }
        await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (!(id.HasValue ? CanEdit : CanCreate) || !user.UserId.HasValue) return Forbid();
        IsEdit = id.HasValue;
        var term = id.HasValue ? await db.SdcTerms.SingleOrDefaultAsync(x => x.Id == id) : null;
        if (id.HasValue && term == null) return NotFound();
        var name = (Input.Name ?? "").Trim();
        if (name.Length == 0) ModelState.AddModelError("Input.Name", "Enter a term name.");
        if (Input.EndDate < Input.StartDate) ModelState.AddModelError("Input.EndDate", "End date must be on or after the start date.");
        if (await db.SdcTerms.AnyAsync(x => x.Name == name && x.Id != (id ?? Guid.Empty))) ModelState.AddModelError("Input.Name", "A committee term with this name already exists.");
        await LoadAsync();
        if (!ModelState.IsValid) return Page();
        if (term == null) { term = new() { Id = Guid.NewGuid(), CreatorId = user.UserId.Value, CreationDate = DateTime.UtcNow }; db.SdcTerms.Add(term); }
        term.Name = name; term.StartDate = Input.StartDate!.Value.Date; term.EndDate = Input.EndDate!.Value.Date; term.IsActive = Input.IsActive;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "The term could not be saved. Check that its name is unique and dates are valid."); return Page(); }
        TempData["SdcSaved"] = "Committee term saved."; return RedirectToPage("./Terms");
    }
    private async Task LoadAsync()
    {
        var query = db.SdcTerms.AsNoTracking().Include(x => x.Members).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchTerm)) query = query.Where(x => x.Name.Contains(SearchTerm));
        if (Status == "active") query = query.Where(x => x.IsActive);
        if (Status == "archived") query = query.Where(x => !x.IsActive);
        Terms = await query.OrderByDescending(x => x.StartDate).ToListAsync();
    }
}
