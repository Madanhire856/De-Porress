using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Student
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public DateTime? Dob { get; set; }

    public int GenderId { get; set; }

    public Guid? ClassId { get; set; }

    public string? BirthEntryNumber { get; set; }

    public int CategoryId { get; set; }

    public string? DisabilityNotesJson { get; set; }

    public string? PhotoUrl { get; set; }

    public bool? IsCatholic { get; set; }

    public int? BaptismStatusId { get; set; }

    public int EnrolmentStatusId { get; set; }

    public Guid? GuardianId { get; set; }

    public DateTime? EnrolmentDate { get; set; }

    public DateTime? CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public Guid? VillageId { get; set; }

    public string? AllergieNotesJson { get; set; }

    public virtual Class? Class { get; set; }

    public virtual User? Creator { get; set; }

    public virtual ICollection<MassRoster> MassRosters { get; set; } = new List<MassRoster>();

    public virtual ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();

    public virtual ICollection<StudentLedger> StudentLedgers { get; set; } = new List<StudentLedger>();

    public virtual Village? Village { get; set; }
}
