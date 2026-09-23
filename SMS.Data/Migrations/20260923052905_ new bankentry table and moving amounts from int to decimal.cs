using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class newbankentrytableandmovingamountsfrominttodecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningBalance",
                table: "StudentLedger",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingBalance",
                table: "StudentLedger",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountUtilised",
                table: "SponsorshipAcquittal",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountDisbursed",
                table: "SponsorshipAcquittal",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "Payment",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseAmount",
                table: "Payment",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payment",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "FeesStructure",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // ============================================================
            //  POST-DECIMAL SETUP
            // ============================================================

            // 1. Align ExchangeRateToBase precision with Payment.ExchangeRate.
            //    Was decimal(18,2) — too tight for rates like 0.0555.
            migrationBuilder.Sql(@"
                ALTER TABLE Currency
                ALTER COLUMN ExchangeRateToBase decimal(18,6) NULL;
            ");

            // 2. Ensure the unique filtered index on IsBase exists.
            //    Idempotent — skips if a previous migration already created it.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'UX_Currency_IsBase'
                      AND object_id = OBJECT_ID('Currency'))
                BEGIN
                    CREATE UNIQUE INDEX UX_Currency_IsBase
                    ON Currency (IsBase)
                    WHERE IsBase = 1;
                END
            ");

            // 3. Seed the base currency.
            //    Adjust codes/rates to match your Currency table.
            //    Safe if a code doesn't exist — the UPDATE is a no-op.
            migrationBuilder.Sql(@"
                UPDATE Currency SET IsBase = 1, ExchangeRateToBase = 1.000000 WHERE Code = 'USD';
                UPDATE Currency SET IsBase = 0, ExchangeRateToBase = 0.055000 WHERE Code = 'ZAR';
                UPDATE Currency SET IsBase = 0, ExchangeRateToBase = 0.037000 WHERE Code = 'ZWG';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the precision change on ExchangeRateToBase.
            // Runs first so the AlterColumns below see the schema as they expect.
            migrationBuilder.Sql(@"
                ALTER TABLE Currency
                ALTER COLUMN ExchangeRateToBase decimal(18,2) NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "OpeningBalance",
                table: "StudentLedger",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "ClosingBalance",
                table: "StudentLedger",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "AmountUtilised",
                table: "SponsorshipAcquittal",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AmountDisbursed",
                table: "SponsorshipAcquittal",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "Payment",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BaseAmount",
                table: "Payment",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Amount",
                table: "Payment",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Amount",
                table: "FeesStructure",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}