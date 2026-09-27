using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    public partial class updatedjsonfieldsinAuditLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            // 1. Drop BOTH immutability triggers up front. They block
            //    the schema changes below, and we are removing them
            //    entirely — they are NOT recreated at the end.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_AuditLog_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER [TR_AuditLog_Immutable];
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_Payments_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER [TR_Payments_Immutable];
            ");

            // 2. Drop the AuditLog table (drops its indexes/FKs with it)
            migrationBuilder.DropTable(
                name: "AuditLog");

            // 3. Drop PrefectNomination
            migrationBuilder.DropTable(
                name: "PrefectNomination");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate PrefectNomination
            migrationBuilder.CreateTable(
                name: "PrefectNomination",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    NominatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostTitleId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrefectNomination", x => x.Id);

                    table.ForeignKey(
                        name: "FK_PrefectNomination_Student",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id");

                    table.ForeignKey(
                        name: "FK_PrefectNomination_User",
                        column: x => x.NominatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrefectNomination_NominatedByUserId",
                table: "PrefectNomination",
                column: "NominatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrefectNomination_StudentId",
                table: "PrefectNomination",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PrefectNomination_StatusId",
                table: "PrefectNomination",
                column: "StatusId");

            // Recreate AuditLog WITHOUT the trigger
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AfterValue = table.Column<string>(type: "json", nullable: true),
                    BeforeValue = table.Column<string>(type: "json", nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TimeStamp",
                table: "AuditLog",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId_TimeStamp",
                table: "AuditLog",
                columns: new[] { "UserId", "TimeStamp" });

            // No trigger recreation — immutability is gone.
        }
    }
}