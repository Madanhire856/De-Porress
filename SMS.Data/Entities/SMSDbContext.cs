using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SMS.Data;

public partial class SMSDbContext : DbContext
{
    public SMSDbContext()
    {
    }

    public SMSDbContext(DbContextOptions<SMSDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activity> Activities { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<Competition> Competitions { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<FeesStructure> FeesStructures { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Guardian> Guardians { get; set; }

    public virtual DbSet<House> Houses { get; set; }

    public virtual DbSet<LiturgicalDay> LiturgicalDays { get; set; }

    public virtual DbSet<MassEvent> MassEvents { get; set; }

    public virtual DbSet<MassRoster> MassRosters { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Prefect> Prefects { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectMilestone> ProjectMilestones { get; set; }

    public virtual DbSet<ProjectRisk> ProjectRisks { get; set; }

    public virtual DbSet<Sponsorship> Sponsorships { get; set; }

    public virtual DbSet<SponsorshipAcquittal> SponsorshipAcquittals { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentGuardian> StudentGuardians { get; set; }

    public virtual DbSet<StudentLedger> StudentLedgers { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<SyncChange> SyncChanges { get; set; }

    public virtual DbSet<TeacherSubject> TeacherSubjects { get; set; }

    public virtual DbSet<Term> Terms { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserGroup> UserGroups { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<Village> Villages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=SMSDb;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activity");

            entity.HasIndex(e => e.CreatorId, "IX_Activity_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.Activities)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Activity_User");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.ToTable("Announcement");

            entity.HasIndex(e => e.CreatorId, "IX_Announcement_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Body).HasColumnType("text");
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Announcement_User");
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("AttendanceRecord");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Date).HasColumnType("datetime");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

            entity.HasIndex(e => e.UserId, "IX_AuditLog_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Action).HasMaxLength(80);
            entity.Property(e => e.AfterValue).HasColumnType("json");
            entity.Property(e => e.BeforeValue).HasColumnType("json");
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.TimeStamp).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditLog_User");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("Class");

            entity.HasIndex(e => e.ClassTeacherId, "IX_Class_ClassTeacherId");

            entity.HasIndex(e => e.CreatorId, "IX_Class_CreatorId");

            entity.HasIndex(e => e.GradeId, "IX_Class_GradeId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(15);

            entity.HasOne(d => d.ClassTeacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.ClassTeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Staff");

            entity.HasOne(d => d.Creator).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_User");

            entity.HasOne(d => d.Grade).WithMany(p => p.Classes)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Class_Grade");
        });

        modelBuilder.Entity<Competition>(entity =>
        {
            entity.ToTable("Competition");

            entity.HasIndex(e => e.CreatorId, "IX_Competition_CreatorId");

            entity.HasIndex(e => e.TermId, "IX_Competition_TermId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Tier).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.Competitions)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competition_User");

            entity.HasOne(d => d.Term).WithMany(p => p.Competitions)
                .HasForeignKey(d => d.TermId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competition_Term");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.Code);

            entity.ToTable("Currency");

            entity.HasIndex(e => e.CreatorId, "IX_Currency_CreatorId");

            entity.Property(e => e.Code).HasMaxLength(5);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(15);
            entity.Property(e => e.Symbol).HasMaxLength(3);

            entity.HasOne(d => d.Creator).WithMany(p => p.Currencies)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Currency_User");
        });

        modelBuilder.Entity<FeesStructure>(entity =>
        {
            entity.ToTable("FeesStructure");

            entity.HasIndex(e => e.CreatorId, "IX_FeesStructure_CreatorId");

            entity.HasIndex(e => e.CurrencyId, "IX_FeesStructure_CurrencyId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.CurrencyId).HasMaxLength(5);

            entity.HasOne(d => d.Creator).WithMany(p => p.FeesStructures)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_FeesStructure_User");

            entity.HasOne(d => d.Currency).WithMany(p => p.FeesStructures)
                .HasForeignKey(d => d.CurrencyId)
                .HasConstraintName("FK_FeesStructure_FeesStructure");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.ToTable("Grade");

            entity.HasIndex(e => e.CreatorId, "IX_Grade_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(10);

            entity.HasOne(d => d.Creator).WithMany(p => p.Grades)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grade_User");
        });

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.ToTable("Guardian");

            entity.HasIndex(e => e.CreatorId, "IX_Guardian_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Mobile).HasMaxLength(15);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Occupation).HasMaxLength(100);
            entity.Property(e => e.Surname).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.Guardians)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Guardian_User");
        });

        modelBuilder.Entity<House>(entity =>
        {
            entity.ToTable("House");

            entity.HasIndex(e => e.CreatorId, "IX_House_CreatorId");

            entity.HasIndex(e => e.MasterId, "IX_House_MasterId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Color).HasMaxLength(15);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(15);

            entity.HasOne(d => d.Creator).WithMany(p => p.Houses)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_House_User");

            entity.HasOne(d => d.Master).WithMany(p => p.Houses)
                .HasForeignKey(d => d.MasterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_House_Staff");
        });

        modelBuilder.Entity<LiturgicalDay>(entity =>
        {
            entity.ToTable("LiturgicalDay");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Color).HasMaxLength(15);
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Rank).HasMaxLength(50);
            entity.Property(e => e.Season).HasMaxLength(50);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(50);
        });

        modelBuilder.Entity<MassEvent>(entity =>
        {
            entity.ToTable("MassEvent");

            entity.HasIndex(e => e.CreatorId, "IX_MassEvent_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Celebrant).HasMaxLength(50);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.DateTime).HasColumnType("datetime");
            entity.Property(e => e.Venue).HasMaxLength(20);

            entity.HasOne(d => d.Creator).WithMany(p => p.MassEvents)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_MassEvent_User");
        });

        modelBuilder.Entity<MassRoster>(entity =>
        {
            entity.ToTable("MassRoster");

            entity.HasIndex(e => e.MassId, "IX_MassRoster_MassId");

            entity.HasIndex(e => e.StaffId, "IX_MassRoster_StaffId");

            entity.HasIndex(e => e.StudentId, "IX_MassRoster_StudentId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");

            entity.HasOne(d => d.Mass).WithMany(p => p.MassRosters)
                .HasForeignKey(d => d.MassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MassRoster_MassEvent");

            entity.HasOne(d => d.Staff).WithMany(p => p.MassRosters)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_MassRoster_Staff");

            entity.HasOne(d => d.Student).WithMany(p => p.MassRosters)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_MassRoster_Student");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");

            entity.HasIndex(e => e.CurrencyId, "IX_Payment_CurrencyId");

            entity.HasIndex(e => e.LedgerId, "IX_Payment_LedgerId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.CurrencyId).HasMaxLength(5);
            entity.Property(e => e.PaymentMethodId)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.ProofOfPaymentUrl).HasMaxLength(20);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(10);

            entity.HasOne(d => d.Currency).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CurrencyId)
                .HasConstraintName("FK_Payment_Currency");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_User");

            entity.HasOne(d => d.Ledger).WithMany(p => p.Payments)
                .HasForeignKey(d => d.LedgerId)
                .HasConstraintName("FK_Payment_StudentLedger");
        });

        modelBuilder.Entity<Prefect>(entity =>
        {
            entity.ToTable("Prefect");

            entity.HasIndex(e => e.CreatorId, "IX_Prefect_CreatorId");

            entity.HasIndex(e => e.NominatorId, "IX_Prefect_NominatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.Prefects)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Prefect_User");

            entity.HasOne(d => d.Nominator).WithMany(p => p.Prefects)
                .HasForeignKey(d => d.NominatorId)
                .HasConstraintName("FK_Prefect_Staff");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Project");

            entity.HasIndex(e => e.CreatorId, "IX_Project_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BudgetAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Project_Staff");

            entity.HasOne(d => d.CreatorNavigation).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Project_User");
        });

        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProjectMileStone");

            entity.ToTable("ProjectMilestone");

            entity.HasIndex(e => e.CreatorId, "IX_ProjectMilestone_CreatorId");

            entity.HasIndex(e => e.ProjectId, "IX_ProjectMilestone_ProjectId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.PhotoUrl).HasMaxLength(50);
            entity.Property(e => e.TargetDate).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.ProjectMilestones)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectMilestone_User");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMilestones)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectMilestone_Project");
        });

        modelBuilder.Entity<ProjectRisk>(entity =>
        {
            entity.ToTable("ProjectRisk");

            entity.HasIndex(e => e.CreatorId, "IX_ProjectRisk_CreatorId");

            entity.HasIndex(e => e.ProjectId, "IX_ProjectRisk_ProjectId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.DescriptionJson).HasColumnType("text");

            entity.HasOne(d => d.Creator).WithMany(p => p.ProjectRisks)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectRisk_User");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectRisks)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectRisk_Project");
        });

        modelBuilder.Entity<Sponsorship>(entity =>
        {
            entity.ToTable("Sponsorship");

            entity.HasIndex(e => e.CreatorId, "IX_Sponsorship_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.SponsorName).HasMaxLength(50);
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.Sponsorships)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Sponsorship_User");
        });

        modelBuilder.Entity<SponsorshipAcquittal>(entity =>
        {
            entity.ToTable("SponsorshipAcquittal");

            entity.HasIndex(e => e.CreatorId, "IX_SponsorshipAcquittal_CreatorId");

            entity.HasIndex(e => e.CurrencyId, "IX_SponsorshipAcquittal_CurrencyId");

            entity.HasIndex(e => e.SponsorshipId, "IX_SponsorshipAcquittal_SponsorshipId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.CurrencyId).HasMaxLength(5);
            entity.Property(e => e.Period).HasMaxLength(15);

            entity.HasOne(d => d.Creator).WithMany(p => p.SponsorshipAcquittals)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SponsorshipAcquittal_User");

            entity.HasOne(d => d.Currency).WithMany(p => p.SponsorshipAcquittals)
                .HasForeignKey(d => d.CurrencyId)
                .HasConstraintName("FK_SponsorshipAcquittal_Currency");

            entity.HasOne(d => d.Sponsorship).WithMany(p => p.SponsorshipAcquittals)
                .HasForeignKey(d => d.SponsorshipId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SponsorshipAcquittal_Sponsorship");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Staff_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DateJoined).HasColumnType("datetime");
            entity.Property(e => e.EcNumber).HasMaxLength(10);
            entity.Property(e => e.IdNumber).HasMaxLength(15);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Surname).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Staff_User");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Student");

            entity.HasIndex(e => e.ClassId, "IX_Student_ClassId");

            entity.HasIndex(e => e.CreatorId, "IX_Student_CreatorId");

            entity.HasIndex(e => e.VillageId, "IX_Student_VillageId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AllergieNotesJson).HasColumnType("text");
            entity.Property(e => e.BirthEntryNumber).HasMaxLength(10);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.DisabilityNotesJson).HasColumnType("text");
            entity.Property(e => e.Dob).HasColumnType("datetime");
            entity.Property(e => e.EnrolmentDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.PhotoUrl).HasMaxLength(50);
            entity.Property(e => e.Surname).HasMaxLength(50);

            entity.HasOne(d => d.Class).WithMany(p => p.Students)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK_Student_Class");

            entity.HasOne(d => d.Creator).WithMany(p => p.Students)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_Student_User");

            entity.HasOne(d => d.Village).WithMany(p => p.Students)
                .HasForeignKey(d => d.VillageId)
                .HasConstraintName("FK_Student_Village");
        });

        modelBuilder.Entity<StudentGuardian>(entity =>
        {
            entity.ToTable("StudentGuardian");

            entity.HasIndex(e => e.GuardianId, "IX_StudentGuardian_GuardianId");

            entity.HasIndex(e => e.StudentId, "IX_StudentGuardian_StudentId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Relationship).HasMaxLength(15);

            entity.HasOne(d => d.Guardian).WithMany(p => p.StudentGuardians)
                .HasForeignKey(d => d.GuardianId)
                .HasConstraintName("FK_StudentGuardian_Guardian");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentGuardians)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_StudentGuardian_Student");
        });

        modelBuilder.Entity<StudentLedger>(entity =>
        {
            entity.ToTable("StudentLedger");

            entity.HasIndex(e => e.CreatorId, "IX_StudentLedger_CreatorId");

            entity.HasIndex(e => e.CurrencyId, "IX_StudentLedger_CurrencyId");

            entity.HasIndex(e => e.StudentId, "IX_StudentLedger_StudentId");

            entity.HasIndex(e => e.TermId, "IX_StudentLedger_TermId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.CurrencyId).HasMaxLength(5);

            entity.HasOne(d => d.Creator).WithMany(p => p.StudentLedgers)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_StudentLedger_User");

            entity.HasOne(d => d.Currency).WithMany(p => p.StudentLedgers)
                .HasForeignKey(d => d.CurrencyId)
                .HasConstraintName("FK_StudentLedger_Currency");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentLedgers)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentLedger_Student");

            entity.HasOne(d => d.Term).WithMany(p => p.StudentLedgers)
                .HasForeignKey(d => d.TermId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentLedger_Term");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subject");

            entity.HasIndex(e => e.CreatorId, "IX_Subject_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subject_User");
        });

        modelBuilder.Entity<SyncChange>(entity =>
        {
            entity.ToTable("SyncChange");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ClientTimeStamp).HasColumnType("datetime");
            entity.Property(e => e.ConflictResolutionNotesJson).HasColumnType("text");
            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.EntityId)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.PayloadJson).HasColumnType("json");
        });

        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            entity.ToTable("TeacherSubject");

            entity.HasIndex(e => e.StaffId, "IX_TeacherSubject_StaffId");

            entity.HasIndex(e => e.SubjectId, "IX_TeacherSubject_SubjectId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Staff).WithMany(p => p.TeacherSubjects)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherSubject_Staff");

            entity.HasOne(d => d.Subject).WithMany(p => p.TeacherSubjects)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherSubject_Subject");
        });

        modelBuilder.Entity<Term>(entity =>
        {
            entity.ToTable("Term");

            entity.HasIndex(e => e.CreatorId, "IX_Term_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.Terms)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Term_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ActivationDate).HasColumnType("datetime");
            entity.Property(e => e.AuthRecoveryCodes).HasMaxLength(256);
            entity.Property(e => e.AuthenticatorKey).HasMaxLength(256);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.LastLoginDate).HasColumnType("datetime");
            entity.Property(e => e.LockoutExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.LoginId).HasMaxLength(50);
            entity.Property(e => e.Mobile).HasMaxLength(15);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("Password_Hash");
            entity.Property(e => e.SecurityStamp).HasMaxLength(256);

            entity.HasOne(d => d.Group).WithMany(p => p.Users)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_User_UserGroup");
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.ToTable("UserGroup");

            entity.HasIndex(e => e.CreatorId, "IX_UserGroup_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.UserGroups)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroup_User");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSession");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.LoginDate).HasColumnType("datetime");
            entity.Property(e => e.LogoutDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Village>(entity =>
        {
            entity.ToTable("Village");

            entity.HasIndex(e => e.CreatorId, "IX_Village_CreatorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Chief).HasMaxLength(50);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Headman).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(20);

            entity.HasOne(d => d.Creator).WithMany(p => p.Villages)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Village_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
