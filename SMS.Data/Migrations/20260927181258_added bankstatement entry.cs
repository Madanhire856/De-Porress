using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedbankstatemententry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankStatementEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyId = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsMatched = table.Column<bool>(type: "bit", nullable: true),
                    MatchedPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    MatchedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankStatementEntry");
        }
    }
}
