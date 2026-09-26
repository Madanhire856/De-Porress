using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class createdBankStatementEntrytable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -----------------------------------------------------------------
            //  Guarded FK drops — the previous failed run may already have
            //  dropped these, so only drop them if they're still present.
            // -----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = 'FK_BankStatementEntry_User'
                             AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        DROP CONSTRAINT FK_BankStatementEntry_User;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = 'FK_BankStatementEntry_User1'
                             AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        DROP CONSTRAINT FK_BankStatementEntry_User1;
            ");

            // -----------------------------------------------------------------
            //  MatchedByUserId: nullable -> NOT NULL
            //
            //  Done with raw SQL because EF's generated DROP INDEX fails when
            //  the index doesn't exist. We guard both DROP INDEX and
            //  CREATE INDEX, and backfill NULLs before the ALTER.
            // -----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_BankStatementEntry_MatchedByUserId'
                             AND object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    DROP INDEX IX_BankStatementEntry_MatchedByUserId
                        ON dbo.BankStatementEntry;

                UPDATE dbo.BankStatementEntry
                SET MatchedByUserId = '00000000-0000-0000-0000-000000000000'
                WHERE MatchedByUserId IS NULL;

                ALTER TABLE dbo.BankStatementEntry
                    ALTER COLUMN MatchedByUserId uniqueidentifier NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_BankStatementEntry_MatchedByUserId'
                                 AND object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    CREATE INDEX IX_BankStatementEntry_MatchedByUserId
                        ON dbo.BankStatementEntry (MatchedByUserId);
            ");

            // -----------------------------------------------------------------
            //  IsMatched: NOT NULL -> nullable
            // -----------------------------------------------------------------
            migrationBuilder.AlterColumn<bool>(
                name: "IsMatched",
                table: "BankStatementEntry",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            // -----------------------------------------------------------------
            //  EntryDate: NOT NULL -> nullable
            // -----------------------------------------------------------------
            migrationBuilder.AlterColumn<DateTime>(
                name: "EntryDate",
                table: "BankStatementEntry",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            // -----------------------------------------------------------------
            //  AuditLog BeforeValue / AfterValue — REMOVED.
            //
            //  This migration was scaffolded to convert these columns back to
            //  the native SQL Server 2025 `json` type. That is exactly what
            //  caused the "JSON text is not properly formatted" errors we just
            //  fixed, and it is inconsistent with the model snapshot which
            //  declares them as nvarchar(max). Do NOT convert them back.
            //
            // migrationBuilder.AlterColumn<string>(
            //     name: "BeforeValue",
            //     table: "AuditLog",
            //     type: "json",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "nvarchar(max)",
            //     oldNullable: true);
            //
            // migrationBuilder.AlterColumn<string>(
            //     name: "AfterValue",
            //     table: "AuditLog",
            //     type: "json",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "nvarchar(max)",
            //     oldNullable: true);

            // -----------------------------------------------------------------
            //  Guarded FK adds — same pattern as the drops.
            //  Note: this migration deliberately swaps which FK name sits on
            //  which column. FK_BankStatementEntry_User goes on CreatorId,
            //  FK_BankStatementEntry_User1 goes on MatchedByUserId.
            // -----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                               WHERE name = 'FK_BankStatementEntry_User'
                                 AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        ADD CONSTRAINT FK_BankStatementEntry_User
                        FOREIGN KEY (CreatorId) REFERENCES dbo.[User] (Id);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                               WHERE name = 'FK_BankStatementEntry_User1'
                                 AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        ADD CONSTRAINT FK_BankStatementEntry_User1
                        FOREIGN KEY (MatchedByUserId) REFERENCES dbo.[User] (Id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // -----------------------------------------------------------------
            //  Guarded FK drops.
            // -----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = 'FK_BankStatementEntry_User'
                             AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        DROP CONSTRAINT FK_BankStatementEntry_User;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = 'FK_BankStatementEntry_User1'
                             AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        DROP CONSTRAINT FK_BankStatementEntry_User1;
            ");

            // -----------------------------------------------------------------
            //  MatchedByUserId: NOT NULL -> nullable (reverse of Up).
            //  Raw SQL for the same reason as in Up.
            // -----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_BankStatementEntry_MatchedByUserId'
                             AND object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    DROP INDEX IX_BankStatementEntry_MatchedByUserId
                        ON dbo.BankStatementEntry;

                ALTER TABLE dbo.BankStatementEntry
                    ALTER COLUMN MatchedByUserId uniqueidentifier NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_BankStatementEntry_MatchedByUserId'
                                 AND object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    CREATE INDEX IX_BankStatementEntry_MatchedByUserId
                        ON dbo.BankStatementEntry (MatchedByUserId);
            ");

            // -----------------------------------------------------------------
            //  IsMatched: nullable -> NOT NULL
            // -----------------------------------------------------------------
            migrationBuilder.AlterColumn<bool>(
                name: "IsMatched",
                table: "BankStatementEntry",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            // -----------------------------------------------------------------
            //  EntryDate: nullable -> NOT NULL
            // -----------------------------------------------------------------
            migrationBuilder.AlterColumn<DateTime>(
                name: "EntryDate",
                table: "BankStatementEntry",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            // -----------------------------------------------------------------
            //  AuditLog BeforeValue / AfterValue — REMOVED (see Up).
            //
            // migrationBuilder.AlterColumn<string>(
            //     name: "BeforeValue",
            //     table: "AuditLog",
            //     type: "nvarchar(max)",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "json",
            //     oldNullable: true);
            //
            // migrationBuilder.AlterColumn<string>(
            //     name: "AfterValue",
            //     table: "AuditLog",
            //     type: "nvarchar(max)",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "json",
            //     oldNullable: true);

            // -----------------------------------------------------------------
            //  Guarded FK adds — reverse mapping of Up.
            //  (Down puts FK_..._User back on MatchedByUserId and
            //   FK_..._User1 back on CreatorId — original layout.)
            // -----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                               WHERE name = 'FK_BankStatementEntry_User'
                                 AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        ADD CONSTRAINT FK_BankStatementEntry_User
                        FOREIGN KEY (MatchedByUserId) REFERENCES dbo.[User] (Id);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                               WHERE name = 'FK_BankStatementEntry_User1'
                                 AND parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry'))
                    ALTER TABLE dbo.BankStatementEntry
                        ADD CONSTRAINT FK_BankStatementEntry_User1
                        FOREIGN KEY (CreatorId) REFERENCES dbo.[User] (Id);
            ");
        }
    }
}