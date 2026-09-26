using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingpaymentdateinPayment : Migration   // ← changed
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -----------------------------------------------------------------
            //  PaymentDate may already exist in the DB (added manually, or by
            //  a previous partial run of this migration), while the EF
            //  history table has no record of it. Guard the ADD COLUMN and
            //  any subsequent ALTER with existence/nullability checks so the
            //  migration is idempotent.
            // -----------------------------------------------------------------

            // 1. Add the column only if it doesn't exist.
            //    Nullable at first so existing rows don't need a sentinel.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.Payment')
                      AND name = 'PaymentDate'
                )
                    ALTER TABLE dbo.Payment
                        ADD PaymentDate datetime NULL;
            ");

            // 2. Backfill any NULLs with a sentinel so we can make the column
            //    NOT NULL, if that's what the model wants.
            //    Adjust the sentinel if you have a more meaningful default.
            migrationBuilder.Sql(@"
                UPDATE dbo.Payment
                SET PaymentDate = '1900-01-01T00:00:00'
                WHERE PaymentDate IS NULL;
            ");

            // 3. Force NOT NULL only if it's currently nullable.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.Payment')
                      AND name = 'PaymentDate'
                      AND is_nullable = 1
                )
                    ALTER TABLE dbo.Payment
                        ALTER COLUMN PaymentDate datetime NOT NULL;
            ");

            // 4. Ensure a DEFAULT exists so future inserts that omit the
            //    column don't fail.
            migrationBuilder.Sql(@"
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
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the default constraint (guarded), then the column (guarded).
            migrationBuilder.Sql(@"
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
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.Payment')
                      AND name = 'PaymentDate'
                )
                    ALTER TABLE dbo.Payment
                        DROP COLUMN PaymentDate;
            ");
        }
    }
}