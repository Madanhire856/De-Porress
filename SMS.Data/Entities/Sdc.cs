namespace SMS.Data;

public class SdcTerm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatorId { get; set; }
    public DateTime CreationDate { get; set; }
    public ICollection<SdcMember> Members { get; set; } = new List<SdcMember>();
}

public class SdcPosition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public Guid CreatorId { get; set; }
    public DateTime CreationDate { get; set; }
    public ICollection<SdcMember> Members { get; set; } = new List<SdcMember>();
}

public class SdcMember
{
    public Guid Id { get; set; }
    public Guid TermId { get; set; }
    public Guid PositionId { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatorId { get; set; }
    public DateTime CreationDate { get; set; }
    public SdcTerm Term { get; set; } = null!;
    public SdcPosition Position { get; set; } = null!;
}
