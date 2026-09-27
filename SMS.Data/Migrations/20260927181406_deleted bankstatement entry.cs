using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class deletedbankstatemententry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankStatementEntry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankStatementEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MatchedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchedPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BankReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsMatched = table.Column<bool>(type: "bit", nullable: true),
                    MatchedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementEntry_Currency",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Code");
                    table.ForeignKey(
                        name: "FK_BankStatementEntry_Payment",
                        column: x => x.MatchedPaymentId,
                        principalTable: "Payment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BankStatementEntry_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BankStatementEntry_User1",
                        column: x => x.MatchedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementEntry_CreatorId",
                table: "BankStatementEntry",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementEntry_CurrencyId",
                table: "BankStatementEntry",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementEntry_MatchedByUserId",
                table: "BankStatementEntry",
                column: "MatchedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementEntry_MatchedPaymentId",
                table: "BankStatementEntry",
                column: "MatchedPaymentId");
        }
    }
}
