using Microsoft.EntityFrameworkCore;
namespace SMS.Data;
public partial class SMSDbContext
{
    public DbSet<SdcTerm> SdcTerms { get; set; }
    public DbSet<SdcPosition> SdcPositions { get; set; }
    public DbSet<SdcMember> SdcMembers { get; set; }
    private static void ConfigureSdc(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SdcTerm>(e =>
        {
            e.ToTable("SdcTerm", t => t.HasCheckConstraint("CK_SdcTerm_Dates", "[EndDate] >= [StartDate]"));
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.StartDate).HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnType("date");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SdcPosition>(e =>
        {
            e.ToTable("SdcPosition");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(80);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SdcMember>(e =>
        {
            e.ToTable("SdcMember");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(100);
            e.HasOne(x => x.Term).WithMany(x => x.Members).HasForeignKey(x => x.TermId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Position).WithMany(x => x.Members).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
