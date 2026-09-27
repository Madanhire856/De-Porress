using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    public partial class correctedreceiptsequesncetable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            // 1. Drop the append-only trigger FIRST. It must go before the
            //    backfill UPDATE below, and we are NOT recreating it.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_Payments_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER [TR_Payments_Immutable];
            ");

            // ─────────────────────────────────────────────────────────────
            // 2. Recreate ReceiptSequence with the correct schema
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.DropTable(
                name: "ReceiptSequece");

            migrationBuilder.CreateTable(
                name: "ReceiptSequence",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    LastIssuedOn = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptSequence", x => x.Year);
                });

            // ─────────────────────────────────────────────────────────────
            // 3. Backfill existing payments with receipt numbers
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                DECLARE @year INT = 2025;
                DECLARE @counter INT = 0;

                UPDATE Payment
                SET @counter = @counter + 1,
                    ReceiptNumber = @counter,
                    ReceiptYear = @year,
                    ExchangeRate = COALESCE(ExchangeRate, 1),
                    BaseAmount = COALESCE(BaseAmount, Amount)
                WHERE ReceiptNumber = 0;

                IF NOT EXISTS (SELECT 1 FROM ReceiptSequence WHERE Year = @year)
                BEGIN
                    INSERT INTO ReceiptSequence (Year, LastNumber, LastIssuedOn)
                    VALUES (@year, @counter, GETDATE());
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ensure the trigger is not recreated — if a future migration
            // ever needs it back, add it there.
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_Payments_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER [TR_Payments_Immutable];
            ");

            migrationBuilder.DropTable(
                name: "ReceiptSequence");

            migrationBuilder.CreateTable(
                name: "ReceiptSequece",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastIssuedOn = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptSequece", x => x.Id);
                });
        }
    }
}