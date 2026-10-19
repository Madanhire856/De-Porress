using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
using SMS.Web.Services;

namespace SMS.Web.Areas.Faith.Pages.Masses;

[Authorize]
public class UpsertModel(SMSDbContext db, WorkspaceAccess access, ICurrentUserService user) : PageModel
{
    public sealed class MassInput
    {
        [Required, DataType(DataType.DateTime)] public DateTime? DateTime { get; set; }
        [Required, DataType(DataType.Date)] public DateOnly? LiturgicalDate { get; set; }
        [Required, StringLength(20)] public string Venue { get; set; } = "";
        [Required, StringLength(50)] public string Celebrant { get; set; } = "";
    }
    [BindProperty] public MassInput Input { get; set; } = new();
    public bool IsEdit { get; private set; }
    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id.HasValue ? !SchoolMasses.CanEdit(access) : !SchoolMasses.CanCreate(access)) return Forbid();
        IsEdit = id.HasValue;
        if (id.HasValue)
        {
            var mass = await db.MassEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (mass == null) return NotFound();
            Input = new() { DateTime = mass.DateTime, LiturgicalDate = mass.LiturgicalDate, Venue = mass.Venue, Celebrant = mass.Celebrant };
        }
        else { var today = DateTime.UtcNow.AddHours(2).Date; Input.DateTime = today.AddHours(8); Input.LiturgicalDate = DateOnly.FromDateTime(today); }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        if ((id.HasValue ? !SchoolMasses.CanEdit(access) : !SchoolMasses.CanCreate(access)) || !user.UserId.HasValue) return Forbid();
        IsEdit = id.HasValue;
        Input.Venue = Input.Venue?.Trim() ?? ""; Input.Celebrant = Input.Celebrant?.Trim() ?? "";
        if (Input.DateTime is null || Input.DateTime.Value.Year < 1753) ModelState.AddModelError("Input.DateTime", "Enter a valid Mass date and time.");
        if (Input.LiturgicalDate is null) ModelState.AddModelError("Input.LiturgicalDate", "Enter the liturgical date.");
        if (string.IsNullOrWhiteSpace(Input.Venue)) ModelState.AddModelError("Input.Venue", "Enter the venue.");
        if (string.IsNullOrWhiteSpace(Input.Celebrant)) ModelState.AddModelError("Input.Celebrant", "Enter the celebrant.");
        if (!ModelState.IsValid) return Page();
        MassEvent mass;
        if (id.HasValue) { mass = (await db.MassEvents.FirstOrDefaultAsync(x => x.Id == id, ct))!; if (mass == null) return NotFound(); }
        else { mass = new() { Id = Guid.NewGuid(), CreatorId = user.UserId, CreationDate = DateTime.UtcNow }; db.MassEvents.Add(mass); }
        mass.DateTime = DateTime.SpecifyKind(Input.DateTime!.Value, DateTimeKind.Unspecified);
        mass.LiturgicalDate = Input.LiturgicalDate!.Value; mass.Venue = Input.Venue; mass.Celebrant = Input.Celebrant;
        await db.SaveChangesAsync(ct);
        TempData["MassStatus"] = "Mass timetable saved.";
        return RedirectToPage("./Details", new { id = mass.Id });
    }
}
