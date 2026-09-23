using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class NameaddedtoTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TermNumber",
                table: "Term",
                newName: "Number");

            migrationBuilder.AddColumn<byte[]>(
                name: "Name",
                table: "Term",
                type: "varbinary(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Term");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "Term",
                newName: "TermNumber");
        }
    }
}
