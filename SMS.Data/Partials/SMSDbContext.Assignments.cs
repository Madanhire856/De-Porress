using Microsoft.EntityFrameworkCore;

namespace SMS.Data;

public partial class SMSDbContext
{
    public DbSet<ClassTeacherAssignment> ClassTeacherAssignments { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigureSdc(modelBuilder);
        ConfigureTrips(modelBuilder);
        // A draft editor must not overwrite a notice published concurrently.
        modelBuilder.Entity<Announcement>().Property(x => x.StatusId).IsConcurrencyToken();
        modelBuilder.Entity<PrefectNomination>(entity =>
        {
            entity.Property(x => x.ReviewNotes).HasMaxLength(1000);
            entity.HasIndex(x => x.PrefectId).IsUnique().HasFilter("[PrefectId] IS NOT NULL");
            entity.HasOne(x => x.Prefect).WithMany().HasForeignKey(x => x.PrefectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
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
