using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Staff
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string EcNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public int Category { get; set; }

    public int Age { get; set; }

    public int MaritalStatus { get; set; }

    public int Gender { get; set; }

    public int Qualification { get; set; }

    public DateTime DateJoined { get; set; }

    public string IdNumber { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<House> Houses { get; set; } = new List<House>();

    public virtual ICollection<MassRoster> MassRosters { get; set; } = new List<MassRoster>();

    public virtual ICollection<Prefect> Prefects { get; set; } = new List<Prefect>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();

    public virtual User User { get; set; } = null!;
}
