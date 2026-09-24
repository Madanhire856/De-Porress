using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingGradeLeveltoGradeLeveldinGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1. Rename GradeLevel to GradeLevelId in Grade table ----
            migrationBuilder.RenameColumn(
                name: "GradeLevel",
                table: "Grade",
                newName: "GradeLevelId");

            // ---- 2. Safely add or alter ExchangeRateToBase on Currency table ----
            migrationBuilder.Sql(@"
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
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ---- Revert ExchangeRateToBase on Currency ----
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Currency') 
                      AND name = 'ExchangeRateToBase'
                )
                BEGIN
                    ALTER TABLE Currency 
                    DROP COLUMN ExchangeRateToBase;
                END
            ");

            // ---- Revert GradeLevelId back to GradeLevel in Grade table ----
            migrationBuilder.RenameColumn(
                name: "GradeLevelId",
                table: "Grade",
                newName: "GradeLevel");
        }
    }
}