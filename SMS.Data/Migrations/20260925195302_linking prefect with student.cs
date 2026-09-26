using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class linkingprefectwithstudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyExchangeRate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ActualRateDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FetchedOn = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyExchangeRate", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prefect_StudentId",
                table: "Prefect",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UX_DailyExchangeRate_CurrencyCode_RequestedDate_RateType",
                table: "DailyExchangeRate",
                columns: new[] { "CurrencyCode", "RequestedDate", "RateType" },
                unique: true,
                filter: "[RateType] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Prefect_Student",
                table: "Prefect",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prefect_Student",
                table: "Prefect");

            migrationBuilder.DropTable(
                name: "DailyExchangeRate");

            migrationBuilder.DropIndex(
                name: "IX_Prefect_StudentId",
                table: "Prefect");
        }
    }
}
