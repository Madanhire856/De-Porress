using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClassTeacherAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassTeacherAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassTeacherAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassTeacherAssignment_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTeacherAssignment_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTeacherAssignment_User_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeacherAssignment_AssignedById",
                table: "ClassTeacherAssignment",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeacherAssignment_ClassId_AcademicYear",
                table: "ClassTeacherAssignment",
                columns: new[] { "ClassId", "AcademicYear" },
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClassTeacherAssignment_StaffId",
                table: "ClassTeacherAssignment",
                column: "StaffId");

            // Carry existing assignments into the current year without guessing historical dates.
            migrationBuilder.Sql("""
                INSERT INTO ClassTeacherAssignment
                    (Id, ClassId, StaffId, AcademicYear, AssignedAt, AssignedById)
                SELECT NEWID(), Id, ClassTeacherId,
                    YEAR(DATEADD(hour, 2, SYSUTCDATETIME())), SYSUTCDATETIME(), CreatorId
                FROM Class WHERE ClassTeacherId IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassTeacherAssignment");
        }
    }
}
