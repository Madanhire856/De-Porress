using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class User
{
    public Guid Id { get; set; }

    public string LoginId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Name { get; set; }

    public string? Mobile { get; set; }

    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; }

    public DateTime? ActivationDate { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public int RoleId { get; set; }

    public Guid? GroupId { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool? TwoFactorAuthEnabled { get; set; }

    public string? SecurityStamp { get; set; }

    public string? AuthRecoveryCodes { get; set; }

    public string? AuthenticatorKey { get; set; }

    public DateTime? LockoutExpiryDate { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    public virtual ICollection<Currency> Currencies { get; set; } = new List<Currency>();

    public virtual ICollection<FeesStructure> FeesStructures { get; set; } = new List<FeesStructure>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual UserGroup? Group { get; set; }

    public virtual ICollection<Guardian> Guardians { get; set; } = new List<Guardian>();

    public virtual ICollection<House> Houses { get; set; } = new List<House>();

    public virtual ICollection<MassEvent> MassEvents { get; set; } = new List<MassEvent>();

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<Prefect> Prefects { get; set; } = new List<Prefect>();

    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    public virtual ICollection<ProjectRisk> ProjectRisks { get; set; } = new List<ProjectRisk>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<SponsorshipAcquittal> SponsorshipAcquittals { get; set; } = new List<SponsorshipAcquittal>();

    public virtual ICollection<Sponsorship> Sponsorships { get; set; } = new List<Sponsorship>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<StudentLedger> StudentLedgers { get; set; } = new List<StudentLedger>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public virtual ICollection<Term> Terms { get; set; } = new List<Term>();

    public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

    public virtual ICollection<Village> Villages { get; set; } = new List<Village>();
}
