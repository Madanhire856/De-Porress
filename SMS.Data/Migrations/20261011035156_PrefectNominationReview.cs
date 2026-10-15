using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrefectNominationReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PrefectId",
                table: "PrefectNomination",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "PrefectNomination",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "PrefectNomination",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "PrefectNomination",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrefectNomination_PrefectId",
                table: "PrefectNomination",
                column: "PrefectId",
                unique: true,
                filter: "[PrefectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PrefectNomination_ReviewedByUserId",
                table: "PrefectNomination",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrefectNomination_Prefect_PrefectId",
                table: "PrefectNomination",
                column: "PrefectId",
                principalTable: "Prefect",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrefectNomination_User_ReviewedByUserId",
                table: "PrefectNomination",
                column: "ReviewedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrefectNomination_Prefect_PrefectId",
                table: "PrefectNomination");

            migrationBuilder.DropForeignKey(
                name: "FK_PrefectNomination_User_ReviewedByUserId",
                table: "PrefectNomination");

            migrationBuilder.DropIndex(
                name: "IX_PrefectNomination_PrefectId",
                table: "PrefectNomination");

            migrationBuilder.DropIndex(
                name: "IX_PrefectNomination_ReviewedByUserId",
                table: "PrefectNomination");

            migrationBuilder.DropColumn(
                name: "PrefectId",
                table: "PrefectNomination");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "PrefectNomination");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "PrefectNomination");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "PrefectNomination");
        }
    }
}
