using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class SportCompetionrelationshipinCompetition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competition_Competition",
                table: "Competition");

            migrationBuilder.AddForeignKey(
                name: "FK_Competition_Competition",
                table: "Competition",
                column: "SportId",
                principalTable: "Sport",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competition_Competition",
                table: "Competition");

            migrationBuilder.AddForeignKey(
                name: "FK_Competition_Competition",
                table: "Competition",
                column: "SportId",
                principalTable: "Competition",
                principalColumn: "Id");
        }
    }
}
