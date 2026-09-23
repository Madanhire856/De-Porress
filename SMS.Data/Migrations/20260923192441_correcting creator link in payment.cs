using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class correctingcreatorlinkinpayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_User",
                table: "Payment");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_CreatorId",
                table: "Payment",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_User",
                table: "Payment",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_User",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_CreatorId",
                table: "Payment");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_User",
                table: "Payment",
                column: "Id",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
