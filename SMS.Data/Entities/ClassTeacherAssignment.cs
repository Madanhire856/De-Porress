namespace SMS.Data;

public class ClassTeacherAssignment
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid StaffId { get; set; }
    public int AcademicYear { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public Guid AssignedById { get; set; }
    public virtual Class Class { get; set; } = null!;
    public virtual Staff Staff { get; set; } = null!;
    public virtual User AssignedBy { get; set; } = null!;
}
