using Microsoft.EntityFrameworkCore;

namespace SMS.Data;

public partial class SMSDbContext
{
    public DbSet<ClassTeacherAssignment> ClassTeacherAssignments { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClassTeacherAssignment>(entity =>
        {
            entity.ToTable("ClassTeacherAssignment");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.ClassId, x.AcademicYear }).IsUnique()
                .HasFilter("[EndedAt] IS NULL");
            entity.HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedBy).WithMany().HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
