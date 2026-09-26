using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class paymentdateaddedtoPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Payment table has an immutable-payments trigger
            // (TR_Payments_Immutable) that blocks UPDATE. We must disable it
            // while backfilling PaymentDate, then re-enable it.
            // Wrapped in TRY/CATCH so a failure cannot leave it disabled.
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                    DISABLE TRIGGER dbo.TR_Payments_Immutable ON dbo.Payment;

                BEGIN TRY

                    -- Add column only if missing.
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.columns
                        WHERE object_id = OBJECT_ID(N'dbo.Payment')
                          AND name = 'PaymentDate'
                    )
                        ALTER TABLE dbo.Payment
                            ADD PaymentDate datetime NULL;

                    -- Backfill NULLs.
                    UPDATE dbo.Payment
                    SET PaymentDate = '1900-01-01T00:00:00'
                    WHERE PaymentDate IS NULL;

                    -- Force NOT NULL only if currently nullable.
                    IF EXISTS (
                        SELECT 1 FROM sys.columns
                        WHERE object_id = OBJECT_ID(N'dbo.Payment')
                          AND name = 'PaymentDate'
                          AND is_nullable = 1
                    )
                        ALTER TABLE dbo.Payment
                            ALTER COLUMN PaymentDate datetime NOT NULL;

                END TRY
                BEGIN CATCH
                    IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                        ENABLE TRIGGER dbo.TR_Payments_Immutable ON dbo.Payment;
                    THROW;
                END CATCH;

                -- Ensure default.
                IF NOT EXISTS (
                    SELECT 1
                    FROM   sys.default_constraints dc
                    JOIN   sys.columns c
                      ON c.object_id = dc.parent_object_id
                     AND c.column_id = dc.parent_column_id
                    WHERE  dc.parent_object_id = OBJECT_ID(N'dbo.Payment')
                      AND  c.name = 'PaymentDate'
                )
                    ALTER TABLE dbo.Payment
                        ADD CONSTRAINT DF_Payment_PaymentDate
                        DEFAULT ('1900-01-01T00:00:00') FOR PaymentDate;

                IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                    ENABLE TRIGGER dbo.TR_Payments_Immutable ON dbo.Payment;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Same trigger handling as Up, in case Down ever needs to run.
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                    DISABLE TRIGGER dbo.TR_Payments_Immutable ON dbo.Payment;

                BEGIN TRY

                    IF EXISTS (
                        SELECT 1
                        FROM   sys.default_constraints dc
                        JOIN   sys.columns c
                          ON c.object_id = dc.parent_object_id
                         AND c.column_id = dc.parent_column_id
                        WHERE  dc.parent_object_id = OBJECT_ID(N'dbo.Payment')
                          AND  c.name = 'PaymentDate'
                    )
                    BEGIN
                        DECLARE @dfname sysname;
                        SELECT @dfname = dc.name
                        FROM   sys.default_constraints dc
                        JOIN   sys.columns c
                          ON c.object_id = dc.parent_object_id
                         AND c.column_id = dc.parent_column_id
                        WHERE  dc.parent_object_id = OBJECT_ID(N'dbo.Payment')
                          AND  c.name = 'PaymentDate';

                        EXEC('ALTER TABLE dbo.Payment DROP CONSTRAINT ' + @dfname);
                    END

                    IF EXISTS (
                        SELECT 1 FROM sys.columns
                        WHERE object_id = OBJECT_ID(N'dbo.Payment')
                          AND name = 'PaymentDate'
                    )
                        ALTER TABLE dbo.Payment
                            DROP COLUMN PaymentDate;

                END TRY
                BEGIN CATCH
                    IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                        ENABLE TRIGGER dbo.TR_Payments_Immutable ON dbo.Payment;
                    THROW;
                END CATCH;

                IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                    ENABLE TRIGGER dbo.TR_Payments_Immutable ON dbo.Payment;
            ");
        }
    }
}