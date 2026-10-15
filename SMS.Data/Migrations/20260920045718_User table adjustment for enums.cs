using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Usertableadjustmentforenums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Qualification",
                table: "Staff",
                newName: "QualificationId");

            migrationBuilder.RenameColumn(
                name: "MaritalStatus",
                table: "Staff",
                newName: "MaritalStatusId");

            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "Staff",
                newName: "GenderId");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Staff",
                newName: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QualificationId",
                table: "Staff",
                newName: "Qualification");

            migrationBuilder.RenameColumn(
                name: "MaritalStatusId",
                table: "Staff",
                newName: "MaritalStatus");

            migrationBuilder.RenameColumn(
                name: "GenderId",
                table: "Staff",
                newName: "Gender");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Staff",
                newName: "Category");
        }
    }
}
