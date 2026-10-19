using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Web.Services;

namespace SMS.Web.Areas.Academics.Pages.Trips;

[Authorize]
public class UpsertModel(SMSDbContext db, SMS.Lib.ICurrentUserService user, WorkspaceAccess access) : PageModel
{
    public class TripInput
    {
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required, StringLength(200)] public string Destination { get; set; } = "";
        [Required, StringLength(2000)] public string Purpose { get; set; } = "";
        [StringLength(2000)] public string? PlacesToPassThrough { get; set; }
        [Required] public Guid? LeaderStaffId { get; set; }
        [Range(typeof(decimal), "0", "999999999.99")] public decimal AmountPerStudent { get; set; }
        [Required, StringLength(5)] public string CurrencyCode { get; set; } = "";
        [DataType(DataType.Date)] public DateOnly? PaymentDueDate { get; set; }
        [Required] public DateTime? DepartureAt { get; set; }
        [Required] public DateTime? ReturnAt { get; set; }
        public List<Guid> GradeIds { get; set; } = [];
        [StringLength(200)] public string? MeetingPoint { get; set; }
        [StringLength(500)] public string? TransportDetails { get; set; }
        [StringLength(500)] public string? Accommodation { get; set; }
        [Range(1, 100000)] public int? Capacity { get; set; }
        [StringLength(100)] public string? EmergencyContactName { get; set; }
        [StringLength(30), Phone] public string? EmergencyContactPhone { get; set; }
        public bool ParentConsentRequired { get; set; } = true;
        [StringLength(4000)] public string? Notes { get; set; }
        public int StatusId { get; set; }
        public DateTime? OriginalUpdatedAt { get; set; }
    }
    [BindProperty] public TripInput Input { get; set; } = new();
    public bool IsEdit { get; private set; }
    public List<SelectListItem> Leaders { get; private set; } = [];
    public List<SelectListItem> Currencies { get; private set; } = [];
    public List<Grade> Grades { get; private set; } = [];
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task LookupsAsync(Guid? existingLeader, CancellationToken ct)
    {
        Leaders = await db.Staff.AsNoTracking().Where(x => x.IsActive == true || x.Id == existingLeader)
            .OrderBy(x => x.Surname).ThenBy(x => x.Name)
            .Select(x => new SelectListItem(x.Surname + " " + x.Name + " · " + x.EcNumber + (x.IsActive == true ? "" : " (inactive)"), x.Id.ToString())).ToListAsync(ct);
        Currencies = await db.Currencies.AsNoTracking().OrderByDescending(x => x.IsBase).ThenBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Code)).ToListAsync(ct);
        Grades = await db.Grades.AsNoTracking().OrderBy(x => x.GradeLevelId).ThenBy(x => x.Name).ToListAsync(ct);
    }
    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (!(id.HasValue ? SchoolTrips.CanEdit(access) : SchoolTrips.CanCreate(access))) return Forbid();
        IsEdit = id.HasValue;
        SchoolTrip? trip = id.HasValue ? await db.SchoolTrips.AsNoTracking().Include(x => x.Grades).SingleOrDefaultAsync(x => x.Id == id, ct) : null;
        if (id.HasValue && trip == null) return NotFound();
        if (trip != null) Input = new() {
            Name = trip.Name, Destination = trip.Destination, Purpose = trip.Purpose, PlacesToPassThrough = trip.PlacesToPassThrough,
            LeaderStaffId = trip.LeaderStaffId, AmountPerStudent = trip.AmountPerStudent, CurrencyCode = trip.CurrencyCode,
            PaymentDueDate = trip.PaymentDueDate, DepartureAt = trip.DepartureAt, ReturnAt = trip.ReturnAt,
            GradeIds = trip.Grades.Select(x => x.GradeId).ToList(), MeetingPoint = trip.MeetingPoint, TransportDetails = trip.TransportDetails,
            Accommodation = trip.Accommodation, Capacity = trip.Capacity, EmergencyContactName = trip.EmergencyContactName,
            EmergencyContactPhone = trip.EmergencyContactPhone, ParentConsentRequired = trip.ParentConsentRequired,
            Notes = trip.Notes, StatusId = trip.StatusId, OriginalUpdatedAt = trip.UpdatedAt
        };
        await LookupsAsync(trip?.LeaderStaffId, ct);
        if (!IsEdit) Input.CurrencyCode = Currencies.FirstOrDefault()?.Value ?? "";
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        if (!(id.HasValue ? SchoolTrips.CanEdit(access) : SchoolTrips.CanCreate(access)) || !user.UserId.HasValue) return Forbid();
        IsEdit = id.HasValue;
        var trip = id.HasValue ? await db.SchoolTrips.Include(x => x.Grades).SingleOrDefaultAsync(x => x.Id == id, ct) : null;
        if (id.HasValue && trip == null) return NotFound();
        await LookupsAsync(trip?.LeaderStaffId, ct);
        // Validate annotations here as well so save invariants do not rely on client validation.
        var validation = new List<ValidationResult>();
        Validator.TryValidateObject(Input, new ValidationContext(Input), validation, true);
        foreach (var error in validation)
            foreach (var name in error.MemberNames.DefaultIfEmpty(""))
                if (!ModelState.TryGetValue("Input." + name, out var entry) || entry.Errors.Count == 0)
                    ModelState.AddModelError("Input." + name, error.ErrorMessage!);
        if (!Enum.IsDefined(typeof(TripStatus), Input.StatusId)) ModelState.AddModelError("Input.StatusId", "Choose a valid trip status.");
        if (Input.AmountPerStudent != decimal.Round(Input.AmountPerStudent, 2)) ModelState.AddModelError("Input.AmountPerStudent", "Use no more than two decimal places.");
        if (!Leaders.Any(x => x.Value == Input.LeaderStaffId?.ToString())) ModelState.AddModelError("Input.LeaderStaffId", "Choose an active staff member as trip leader.");
        if (!Currencies.Any(x => x.Value == Input.CurrencyCode)) ModelState.AddModelError("Input.CurrencyCode", "Choose a configured currency.");
        Input.GradeIds = (Input.GradeIds ?? []).Distinct().ToList();
        if (Input.GradeIds.Count == 0 || Input.GradeIds.Any(id => !Grades.Any(g => g.Id == id))) ModelState.AddModelError("Input.GradeIds", "Choose at least one valid grade.");
        if (Input.DepartureAt.HasValue && Input.ReturnAt.HasValue && Input.ReturnAt <= Input.DepartureAt) ModelState.AddModelError("Input.ReturnAt", "Return must be after departure.");
        if (Input.AmountPerStudent > 0 && !Input.PaymentDueDate.HasValue) ModelState.AddModelError("Input.PaymentDueDate", "Enter the payment due date for this paid trip.");
        if (Input.PaymentDueDate.HasValue && Input.DepartureAt.HasValue && Input.PaymentDueDate > DateOnly.FromDateTime(Input.DepartureAt.Value)) ModelState.AddModelError("Input.PaymentDueDate", "Payment must be due on or before departure.");
        if ((Clean(Input.EmergencyContactName) == null) != (Clean(Input.EmergencyContactPhone) == null)) ModelState.AddModelError("Input.EmergencyContactPhone", "Enter both the emergency contact name and phone number, or leave both blank.");
        if (trip != null && trip.UpdatedAt != Input.OriginalUpdatedAt) ModelState.AddModelError("", "This trip was changed by another administrator. Reload it before saving.");
        if (!ModelState.IsValid) return Page();
        if (trip == null)
        {
            trip = new() { Id = Guid.NewGuid(), CreatorId = user.UserId.Value, CreationDate = DateTime.UtcNow };
            db.SchoolTrips.Add(trip);
        }
        trip.Name = Input.Name.Trim(); trip.Destination = Input.Destination.Trim(); trip.Purpose = Input.Purpose.Trim();
        trip.PlacesToPassThrough = Clean(Input.PlacesToPassThrough); trip.LeaderStaffId = Input.LeaderStaffId!.Value;
        trip.AmountPerStudent = Input.AmountPerStudent; trip.CurrencyCode = Input.CurrencyCode; trip.PaymentDueDate = Input.PaymentDueDate;
        trip.DepartureAt = DateTime.SpecifyKind(Input.DepartureAt!.Value, DateTimeKind.Unspecified);
        trip.ReturnAt = DateTime.SpecifyKind(Input.ReturnAt!.Value, DateTimeKind.Unspecified);
        trip.MeetingPoint = Clean(Input.MeetingPoint); trip.TransportDetails = Clean(Input.TransportDetails); trip.Accommodation = Clean(Input.Accommodation);
        trip.Capacity = Input.Capacity; trip.EmergencyContactName = Clean(Input.EmergencyContactName); trip.EmergencyContactPhone = Clean(Input.EmergencyContactPhone);
        trip.ParentConsentRequired = Input.ParentConsentRequired; trip.Notes = Clean(Input.Notes); trip.StatusId = Input.StatusId;
        trip.UpdatedById = user.UserId.Value; trip.UpdatedAt = DateTime.UtcNow;
        foreach (var row in trip.Grades.Where(x => !Input.GradeIds.Contains(x.GradeId)).ToList()) db.SchoolTripGrades.Remove(row);
        foreach (var gradeId in Input.GradeIds.Where(id => !trip.Grades.Any(x => x.GradeId == id))) trip.Grades.Add(new() { TripId = trip.Id, GradeId = gradeId });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "This trip changed while you were saving. Reload it and try again."); return Page(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "The trip could not be saved. Check that the staff member, grades and currency still exist, then try again."); return Page(); }
        TempData["TripSaved"] = "Trip saved.";
        return RedirectToPage("./Details", new { id = trip.Id });
    }
}
