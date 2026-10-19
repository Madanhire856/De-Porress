using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Data;

public class SchoolTrip
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string? PlacesToPassThrough { get; set; }
    public Guid LeaderStaffId { get; set; }
    public Staff Leader { get; set; } = null!;
    public decimal AmountPerStudent { get; set; }
    public string CurrencyCode { get; set; } = "";
    public Currency Currency { get; set; } = null!;
    public DateOnly? PaymentDueDate { get; set; }
    public DateTime DepartureAt { get; set; }
    public DateTime ReturnAt { get; set; }
    [NotMapped] public int Days => Math.Max(1, (ReturnAt.Date - DepartureAt.Date).Days + 1);
    [NotMapped] public int Nights => Math.Max(0, (ReturnAt.Date - DepartureAt.Date).Days);
    public string? MeetingPoint { get; set; }
    public string? TransportDetails { get; set; }
    public string? Accommodation { get; set; }
    public int? Capacity { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool ParentConsentRequired { get; set; } = true;
    public string? Notes { get; set; }
    public int StatusId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime CreationDate { get; set; }
    public Guid UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<SchoolTripGrade> Grades { get; set; } = new List<SchoolTripGrade>();
}

public class SchoolTripGrade
{
    public Guid TripId { get; set; }
    public SchoolTrip Trip { get; set; } = null!;
    public Guid GradeId { get; set; }
    public Grade Grade { get; set; } = null!;
}
