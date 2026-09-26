using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatedjsonfieldsinAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrefectNomination");

            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "json",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "json",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

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
                name: "IX_PrefectNomination_StatusId",
                table: "PrefectNomination",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PrefectNomination_StudentId",
                table: "PrefectNomination",
                column: "StudentId");
        }
    }
}
