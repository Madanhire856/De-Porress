using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    public partial class correctedreceiptsequesncetable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            // ---- CUSTOM — backfill existing payments with receipt numbers ----
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

            // ---- CUSTOM — drop trigger in its own batch ----
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_Payments_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER TR_Payments_Immutable;
            ");

            // ---- CUSTOM — create trigger in its own batch ----
            // SQL Server requires CREATE TRIGGER to be the first statement
            // in a batch, so it cannot share a Sql() call with anything else.
            migrationBuilder.Sql(@"
                CREATE TRIGGER TR_Payments_Immutable
                ON Payment
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    RAISERROR('Payments table is append-only. Create a reversal entry instead.', 16, 1);
                    ROLLBACK TRANSACTION;
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ---- CUSTOM — drop trigger first ----
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Payments_Immutable;");

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