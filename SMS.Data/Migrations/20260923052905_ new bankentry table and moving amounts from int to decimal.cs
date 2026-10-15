using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    public partial class newbankentrytableandmovingamountsfrominttodecimal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            //  PRE-DECIMAL SETUP — Safely Add IsBase and ExchangeRateToBase
            // ============================================================
            migrationBuilder.Sql(@"
                -- 1. Ensure ExchangeRateToBase exists
                IF NOT EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Currency') 
                      AND name = 'ExchangeRateToBase'
                )
                BEGIN
                    ALTER TABLE Currency 
                    ADD ExchangeRateToBase decimal(18,6) NULL;
                END
                ELSE
                BEGIN
                    DECLARE @dfName sysname;

                    SELECT @dfName = dc.name
                    FROM sys.default_constraints dc
                    JOIN sys.columns c
                      ON dc.parent_column_id = c.column_id
                     AND dc.parent_object_id = c.object_id
                    WHERE dc.parent_object_id = OBJECT_ID('Currency')
                      AND c.name = 'ExchangeRateToBase';

                    IF @dfName IS NOT NULL
                        EXEC('ALTER TABLE Currency DROP CONSTRAINT ' + @dfName + ';');

                    ALTER TABLE Currency
                    ALTER COLUMN ExchangeRateToBase decimal(18,6) NULL;
                END;

                -- 2. Ensure IsBase exists
                IF NOT EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Currency') 
                      AND name = 'IsBase'
                )
                BEGIN
                    ALTER TABLE Currency 
                    ADD IsBase bit NOT NULL DEFAULT 0;
                END;
            ");

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
            //  POST-DECIMAL SETUP — Drop existing index, then seed data
            // ============================================================

            migrationBuilder.Sql(@"
                -- Clean up existing index if present to avoid Error 1913
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'UX_Currency_IsBase'
                      AND object_id = OBJECT_ID('Currency'))
                BEGIN
                    DROP INDEX UX_Currency_IsBase ON Currency;
                END;

                CREATE UNIQUE INDEX UX_Currency_IsBase
                ON Currency (IsBase)
                WHERE IsBase = 1;
            ");

            migrationBuilder.Sql(@"
                UPDATE Currency SET IsBase = 1, ExchangeRateToBase = 1.000000 WHERE Code = 'USD';
                UPDATE Currency SET IsBase = 0, ExchangeRateToBase = 0.055000 WHERE Code = 'ZAR';
                UPDATE Currency SET IsBase = 0, ExchangeRateToBase = 0.037000 WHERE Code = 'ZWG';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Drop Unique Index
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'UX_Currency_IsBase'
                      AND object_id = OBJECT_ID('Currency'))
                BEGIN
                    DROP INDEX UX_Currency_IsBase ON Currency;
                END;

                -- Remove IsBase
                IF EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Currency') 
                      AND name = 'IsBase'
                )
                BEGIN
                    ALTER TABLE Currency DROP COLUMN IsBase;
                END;

                -- Revert ExchangeRateToBase
                IF EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Currency') 
                      AND name = 'ExchangeRateToBase'
                )
                BEGIN
                    DECLARE @dfName sysname;

                    SELECT @dfName = dc.name
                    FROM sys.default_constraints dc
                    JOIN sys.columns c
                      ON dc.parent_column_id = c.column_id
                     AND dc.parent_object_id = c.object_id
                    WHERE dc.parent_object_id = OBJECT_ID('Currency')
                      AND c.name = 'ExchangeRateToBase';

                    IF @dfName IS NOT NULL
                        EXEC('ALTER TABLE Currency DROP CONSTRAINT ' + @dfName + ';');

                    ALTER TABLE Currency
                    ALTER COLUMN ExchangeRateToBase decimal(18,2) NULL;
                END;
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