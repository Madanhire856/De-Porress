using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditlogUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -----------------------------------------------------------------
            //  AUDIT LOG NORMALISATION
            //  See earlier notes: '{}' not 'null' (native json parser);
            //  CONVERT(nvarchar(max), ...) for text/nvarchar comparison;
            //  trigger disabled here and re-enabled at the very end of Up.
            // -----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_AuditLog_Immutable', N'TR') IS NOT NULL
                    DISABLE TRIGGER dbo.TR_AuditLog_Immutable ON dbo.AuditLog;

                UPDATE dbo.AuditLog
                SET BeforeValue =
                    CASE
                        WHEN BeforeValue IS NULL
                            THEN N'{}'
                        WHEN CONVERT(nvarchar(max), BeforeValue) = N'null'
                            THEN N'{}'
                        WHEN ISJSON(CONVERT(nvarchar(max), BeforeValue), VALUE) = 1
                            THEN CONVERT(nvarchar(max), BeforeValue)
                        ELSE N'""' + STRING_ESCAPE(CONVERT(nvarchar(max), BeforeValue), 'json') + N'""'
                    END
                WHERE BeforeValue IS NULL
                   OR CONVERT(nvarchar(max), BeforeValue) = N'null'
                   OR ISNULL(ISJSON(CONVERT(nvarchar(max), BeforeValue), VALUE), 0) = 0;

                UPDATE dbo.AuditLog
                SET AfterValue =
                    CASE
                        WHEN AfterValue IS NULL
                            THEN N'{}'
                        WHEN CONVERT(nvarchar(max), AfterValue) = N'null'
                            THEN N'{}'
                        WHEN ISJSON(CONVERT(nvarchar(max), AfterValue), VALUE) = 1
                            THEN CONVERT(nvarchar(max), AfterValue)
                        ELSE N'""' + STRING_ESCAPE(CONVERT(nvarchar(max), AfterValue), 'json') + N'""'
                    END
                WHERE AfterValue IS NULL
                   OR CONVERT(nvarchar(max), AfterValue) = N'null'
                   OR ISNULL(ISJSON(CONVERT(nvarchar(max), AfterValue), VALUE), 0) = 0;
            ");

            // ---- DDL. Trigger stays disabled for the duration. ----

            migrationBuilder.AlterColumn<bool>(
                name: "IsBase",
                table: "Currency",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuditLog",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // ---- PK on BankStatementEntry (guarded; already exists) ----
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM   sys.key_constraints
                    WHERE  parent_object_id = OBJECT_ID(N'dbo.BankStatementEntry')
                      AND  type = 'PK'
                )
                BEGIN
                    ALTER TABLE dbo.BankStatementEntry
                        ADD CONSTRAINT PK_BankStatementEntry PRIMARY KEY (Id);
                END
            ");

            // ---- AuditLog indexes (guarded; already created by the
            //      'Audit adjustments' migration) ----
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_AuditLog_EntityType_EntityId'
                      AND object_id = OBJECT_ID(N'dbo.AuditLog')
                )
                    CREATE INDEX IX_AuditLog_EntityType_EntityId
                        ON dbo.AuditLog (EntityType, EntityId);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_AuditLog_TimeStamp'
                      AND object_id = OBJECT_ID(N'dbo.AuditLog')
                )
                    CREATE INDEX IX_AuditLog_TimeStamp
                        ON dbo.AuditLog (TimeStamp);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_AuditLog_UserId_TimeStamp'
                      AND object_id = OBJECT_ID(N'dbo.AuditLog')
                )
                    CREATE INDEX IX_AuditLog_UserId_TimeStamp
                        ON dbo.AuditLog (UserId, TimeStamp);
            ");

            // ---- Re-enable immutable trigger now that DDL is done ----
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_AuditLog_Immutable', N'TR') IS NOT NULL
                    ENABLE TRIGGER dbo.TR_AuditLog_Immutable ON dbo.AuditLog;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Disable trigger around the AlterColumns below.
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_AuditLog_Immutable', N'TR') IS NOT NULL
                    DISABLE TRIGGER dbo.TR_AuditLog_Immutable ON dbo.AuditLog;
            ");

            // NOTE: Do NOT drop PK_BankStatementEntry here — it belongs to an
            // earlier migration. Do NOT drop the three AuditLog indexes here
            // either — they were created by the 'Audit adjustments' migration.
            //
            // migrationBuilder.DropPrimaryKey(name: "PK_BankStatementEntry", table: "BankStatementEntry");
            // migrationBuilder.DropIndex(name: "IX_AuditLog_EntityType_EntityId", table: "AuditLog");
            // migrationBuilder.DropIndex(name: "IX_AuditLog_TimeStamp", table: "AuditLog");
            // migrationBuilder.DropIndex(name: "IX_AuditLog_UserId_TimeStamp", table: "AuditLog");

            migrationBuilder.AlterColumn<bool>(
                name: "IsBase",
                table: "Currency",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuditLog",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Re-enable trigger.
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_AuditLog_Immutable', N'TR') IS NOT NULL
                    ENABLE TRIGGER dbo.TR_AuditLog_Immutable ON dbo.AuditLog;
            ");
        }
    }
}