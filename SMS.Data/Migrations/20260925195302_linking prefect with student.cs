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
            // -----------------------------------------------------------------
            //  Every DDL statement is guarded so this migration is idempotent
            //  against a database that may already have some or all of the
            //  objects it wants to create.
            // -----------------------------------------------------------------

            // ---- 1. DailyExchangeRate table ----
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DailyExchangeRate')
                BEGIN
                    CREATE TABLE dbo.DailyExchangeRate (
                        Id             uniqueidentifier NOT NULL,
                        CurrencyCode   nvarchar(10)     NOT NULL,
                        RequestedDate  datetime         NOT NULL,
                        ActualRateDate datetime         NOT NULL,
                        Rate           decimal(18,6)    NOT NULL,
                        Source         nvarchar(20)     NOT NULL,
                        RateType       nvarchar(20)     NULL,
                        FetchedOn      datetime         NOT NULL,
                        CONSTRAINT PK_DailyExchangeRate PRIMARY KEY (Id)
                    );
                END
            ");

            // ---- 2. Index on Prefect.StudentId ----
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Prefect_StudentId'
                      AND object_id = OBJECT_ID(N'dbo.Prefect')
                )
                    CREATE INDEX IX_Prefect_StudentId
                        ON dbo.Prefect (StudentId);
            ");

            // ---- 3. Unique filtered index on DailyExchangeRate ----
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'UX_DailyExchangeRate_CurrencyCode_RequestedDate_RateType'
                      AND object_id = OBJECT_ID(N'dbo.DailyExchangeRate')
                )
                    CREATE UNIQUE INDEX UX_DailyExchangeRate_CurrencyCode_RequestedDate_RateType
                        ON dbo.DailyExchangeRate (CurrencyCode, RequestedDate, RateType)
                        WHERE RateType IS NOT NULL;
            ");

            // ---- 4. FK Prefect.StudentId -> Student.Id ----
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_Prefect_Student'
                      AND parent_object_id = OBJECT_ID(N'dbo.Prefect')
                )
                    ALTER TABLE dbo.Prefect
                        ADD CONSTRAINT FK_Prefect_Student
                        FOREIGN KEY (StudentId) REFERENCES dbo.Student (Id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in reverse order, all guarded.

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_Prefect_Student'
                      AND parent_object_id = OBJECT_ID(N'dbo.Prefect')
                )
                    ALTER TABLE dbo.Prefect
                        DROP CONSTRAINT FK_Prefect_Student;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'UX_DailyExchangeRate_CurrencyCode_RequestedDate_RateType'
                      AND object_id = OBJECT_ID(N'dbo.DailyExchangeRate')
                )
                    DROP INDEX UX_DailyExchangeRate_CurrencyCode_RequestedDate_RateType
                        ON dbo.DailyExchangeRate;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Prefect_StudentId'
                      AND object_id = OBJECT_ID(N'dbo.Prefect')
                )
                    DROP INDEX IX_Prefect_StudentId
                        ON dbo.Prefect;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DailyExchangeRate')
                    DROP TABLE dbo.DailyExchangeRate;
            ");
        }
    }
}