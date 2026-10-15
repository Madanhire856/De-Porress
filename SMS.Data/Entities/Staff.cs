using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Staff
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? EcNumber { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public int CategoryId { get; set; }

    public int Age { get; set; }

    public int MaritalStatusId { get; set; }

    public int GenderId { get; set; }

    public int QualificationId { get; set; }

    public DateTime DateJoined { get; set; }

    public string IdNumber { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<HouseTeacher> HouseTeachers { get; set; } = new List<HouseTeacher>();

    public virtual ICollection<MassRoster> MassRosters { get; set; } = new List<MassRoster>();

    public virtual ICollection<Prefect> Prefects { get; set; } = new List<Prefect>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<SportTeacher> SportTeachers { get; set; } = new List<SportTeacher>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();

    public virtual User? User { get; set; }
}
