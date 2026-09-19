using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Guardian
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public int? Age { get; set; }

    public int GenderId { get; set; }

    public string? Occupation { get; set; }

    public Guid? VillageId { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public virtual User? Creator { get; set; }

    public virtual ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
}
