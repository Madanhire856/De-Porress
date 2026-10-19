using Microsoft.EntityFrameworkCore;

namespace SMS.Data;

public partial class SMSDbContext
{
    public DbSet<SchoolTrip> SchoolTrips { get; set; }
    public DbSet<SchoolTripGrade> SchoolTripGrades { get; set; }
    private static void ConfigureTrips(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchoolTrip>(e =>
        {
            e.ToTable("SchoolTrip", t => {
                t.HasCheckConstraint("CK_SchoolTrip_Dates", "[ReturnAt] > [DepartureAt]");
                t.HasCheckConstraint("CK_SchoolTrip_Amount", "[AmountPerStudent] >= 0");
                t.HasCheckConstraint("CK_SchoolTrip_Capacity", "[Capacity] IS NULL OR [Capacity] > 0");
                t.HasCheckConstraint("CK_SchoolTrip_Status", "[StatusId] IN (0,1,2,3)");
                t.HasCheckConstraint("CK_SchoolTrip_DueDate", "([AmountPerStudent] = 0 OR [PaymentDueDate] IS NOT NULL) AND ([PaymentDueDate] IS NULL OR [PaymentDueDate] <= CAST([DepartureAt] AS date))");
            });
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Destination).HasMaxLength(200);
            e.Property(x => x.Purpose).HasMaxLength(2000);
            e.Property(x => x.PlacesToPassThrough).HasMaxLength(2000);
            e.Property(x => x.AmountPerStudent).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).HasMaxLength(5);
            e.Property(x => x.PaymentDueDate).HasColumnType("date");
            e.Property(x => x.MeetingPoint).HasMaxLength(200);
            e.Property(x => x.TransportDetails).HasMaxLength(500);
            e.Property(x => x.Accommodation).HasMaxLength(500);
            e.Property(x => x.EmergencyContactName).HasMaxLength(100);
            e.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasIndex(x => new { x.StatusId, x.DepartureAt });
            e.Property(x => x.UpdatedAt).IsConcurrencyToken();
            e.HasOne(x => x.Leader).WithMany().HasForeignKey(x => x.LeaderStaffId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedById).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SchoolTripGrade>(e =>
        {
            e.ToTable("SchoolTripGrade");
            e.HasKey(x => new { x.TripId, x.GradeId });
            e.HasOne(x => x.Trip).WithMany(x => x.Grades).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Grade).WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
