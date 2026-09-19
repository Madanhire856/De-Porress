using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CorrectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiturgicalDay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Season = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Rank = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiturgicalDay", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncChange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: false),
                    PayloadJson = table.Column<string>(type: "json", nullable: false),
                    ClientTimeStamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    SyncStatusId = table.Column<int>(type: "int", nullable: false),
                    ConflictResolutionNotesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncChange", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Password_Hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorAuthEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuthRecoveryCodes = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuthenticatorKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LockoutExpiryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSession",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LogoutDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activity_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcement_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BeforeValue = table.Column<string>(type: "json", nullable: false),
                    AfterValue = table.Column<string>(type: "json", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Currency_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Grade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GradeLevel = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grade_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Guardian",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    GenderId = table.Column<int>(type: "int", nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VillageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardian", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guardian_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MassEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiturgicalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Celebrant = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MassEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MassEvent_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sponsorship",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorTypeId = table.Column<int>(type: "int", nullable: true),
                    SponsorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PercentageCoverage = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsorship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sponsorship_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EcNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    MaritalStatus = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Qualification = table.Column<int>(type: "int", nullable: false),
                    DateJoined = table.Column<DateTime>(type: "datetime", nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subject_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Term",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    TermNumber = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Term", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Term_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroup_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Village",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Headman = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Chief = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Village", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Village_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeesStructure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevyTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeesStructure", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeesStructure_FeesStructure",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Code");
                    table.ForeignKey(
                        name: "FK_FeesStructure_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipAcquittal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    AmountDisbursed = table.Column<int>(type: "int", nullable: true),
                    AmountUtilised = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipAcquittal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipAcquittal_Currency",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Code");
                    table.ForeignKey(
                        name: "FK_SponsorshipAcquittal_Sponsorship",
                        column: x => x.SponsorshipId,
                        principalTable: "Sponsorship",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SponsorshipAcquittal_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Class",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    ClassTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Class", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Class_Grade",
                        column: x => x.GradeId,
                        principalTable: "Grade",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Class_Staff",
                        column: x => x.ClassTeacherId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Class_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "House",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    MasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_House", x => x.Id);
                    table.ForeignKey(
                        name: "FK_House_Staff",
                        column: x => x.MasterId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_House_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Prefect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostTitleId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    NominatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prefect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prefect_Staff",
                        column: x => x.NominatorId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prefect_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    ResponsiblePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_Staff",
                        column: x => x.CreatorId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Project_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubject",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubject_Staff",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeacherSubject_Subject",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Competition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competition_Term",
                        column: x => x.TermId,
                        principalTable: "Term",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Competition_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Dob = table.Column<DateTime>(type: "datetime", nullable: true),
                    GenderId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BirthEntryNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    DisabilityNotesJson = table.Column<string>(type: "text", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsCatholic = table.Column<bool>(type: "bit", nullable: true),
                    BaptismStatusId = table.Column<int>(type: "int", nullable: true),
                    EnrolmentStatusId = table.Column<int>(type: "int", nullable: false),
                    GuardianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VillageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllergieNotesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Student_Class",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Student_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Student_Village",
                        column: x => x.VillageId,
                        principalTable: "Village",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectMilestone",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    PercentageComplete = table.Column<int>(type: "int", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMileStone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMilestone_Project",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectMilestone_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectRisk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DescriptionJson = table.Column<string>(type: "text", nullable: false),
                    EscalationLevelId = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRisk", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRisk_Project",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectRisk_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MassRoster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MassRoster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MassRoster_MassEvent",
                        column: x => x.MassId,
                        principalTable: "MassEvent",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassRoster_Staff",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MassRoster_Student",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentGuardian",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuardianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Relationship = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGuardian", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGuardian_Guardian",
                        column: x => x.GuardianId,
                        principalTable: "Guardian",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentGuardian_Student",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpeningBalance = table.Column<int>(type: "int", nullable: false),
                    ClosingBalance = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentLedger_Currency",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Code");
                    table.ForeignKey(
                        name: "FK_StudentLedger_Student",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentLedger_Term",
                        column: x => x.TermId,
                        principalTable: "Term",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentLedger_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    PaymentMethodId = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ProofOfPaymentUrl = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_Currency",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Code");
                    table.ForeignKey(
                        name: "FK_Payment_StudentLedger",
                        column: x => x.LedgerId,
                        principalTable: "StudentLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payment_User",
                        column: x => x.Id,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_CreatorId",
                table: "Activity",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_CreatorId",
                table: "Announcement",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Class_ClassTeacherId",
                table: "Class",
                column: "ClassTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Class_CreatorId",
                table: "Class",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Class_GradeId",
                table: "Class",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Competition_CreatorId",
                table: "Competition",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Competition_TermId",
                table: "Competition",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_CreatorId",
                table: "Currency",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FeesStructure_CreatorId",
                table: "FeesStructure",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FeesStructure_CurrencyId",
                table: "FeesStructure",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Grade_CreatorId",
                table: "Grade",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Guardian_CreatorId",
                table: "Guardian",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_House_CreatorId",
                table: "House",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_House_MasterId",
                table: "House",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_MassEvent_CreatorId",
                table: "MassEvent",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_MassRoster_MassId",
                table: "MassRoster",
                column: "MassId");

            migrationBuilder.CreateIndex(
                name: "IX_MassRoster_StaffId",
                table: "MassRoster",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_MassRoster_StudentId",
                table: "MassRoster",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_CurrencyId",
                table: "Payment",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_LedgerId",
                table: "Payment",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_Prefect_CreatorId",
                table: "Prefect",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prefect_NominatorId",
                table: "Prefect",
                column: "NominatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_CreatorId",
                table: "Project",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestone_CreatorId",
                table: "ProjectMilestone",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestone_ProjectId",
                table: "ProjectMilestone",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRisk_CreatorId",
                table: "ProjectRisk",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRisk_ProjectId",
                table: "ProjectRisk",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorship_CreatorId",
                table: "Sponsorship",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAcquittal_CreatorId",
                table: "SponsorshipAcquittal",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAcquittal_CurrencyId",
                table: "SponsorshipAcquittal",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipAcquittal_SponsorshipId",
                table: "SponsorshipAcquittal",
                column: "SponsorshipId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_UserId",
                table: "Staff",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_ClassId",
                table: "Student",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_CreatorId",
                table: "Student",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_VillageId",
                table: "Student",
                column: "VillageId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGuardian_GuardianId",
                table: "StudentGuardian",
                column: "GuardianId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGuardian_StudentId",
                table: "StudentGuardian",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLedger_CreatorId",
                table: "StudentLedger",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLedger_CurrencyId",
                table: "StudentLedger",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLedger_StudentId",
                table: "StudentLedger",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLedger_TermId",
                table: "StudentLedger",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_Subject_CreatorId",
                table: "Subject",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubject_StaffId",
                table: "TeacherSubject",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubject_SubjectId",
                table: "TeacherSubject",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Term_CreatorId",
                table: "Term",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroup_CreatorId",
                table: "UserGroup",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Village_CreatorId",
                table: "Village",
                column: "CreatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activity");

            migrationBuilder.DropTable(
                name: "Announcement");

            migrationBuilder.DropTable(
                name: "AttendanceRecord");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "Competition");

            migrationBuilder.DropTable(
                name: "FeesStructure");

            migrationBuilder.DropTable(
                name: "House");

            migrationBuilder.DropTable(
                name: "LiturgicalDay");

            migrationBuilder.DropTable(
                name: "MassRoster");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "Prefect");

            migrationBuilder.DropTable(
                name: "ProjectMilestone");

            migrationBuilder.DropTable(
                name: "ProjectRisk");

            migrationBuilder.DropTable(
                name: "SponsorshipAcquittal");

            migrationBuilder.DropTable(
                name: "StudentGuardian");

            migrationBuilder.DropTable(
                name: "SyncChange");

            migrationBuilder.DropTable(
                name: "TeacherSubject");

            migrationBuilder.DropTable(
                name: "UserGroup");

            migrationBuilder.DropTable(
                name: "UserSession");

            migrationBuilder.DropTable(
                name: "MassEvent");

            migrationBuilder.DropTable(
                name: "StudentLedger");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "Sponsorship");

            migrationBuilder.DropTable(
                name: "Guardian");

            migrationBuilder.DropTable(
                name: "Subject");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "Term");

            migrationBuilder.DropTable(
                name: "Class");

            migrationBuilder.DropTable(
                name: "Village");

            migrationBuilder.DropTable(
                name: "Grade");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
