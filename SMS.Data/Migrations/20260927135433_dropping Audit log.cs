using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class droppingAuditlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            //  AuditLog may already have been dropped by an earlier migration
            //  (20260926071727_updated json fields in AuditLog). Guard the
            //  drop so this migration succeeds whether or not it still
            //  exists.
            //
            //  Also drop TR_AuditLog_Immutable defensively in case a partial
            //  earlier run left it behind — DROP TABLE would fail anyway if
            //  a trigger was attached, and this makes intent explicit.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_AuditLog_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER [TR_AuditLog_Immutable];

                IF OBJECT_ID('AuditLog', 'U') IS NOT NULL
                    DROP TABLE [AuditLog];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            //  Recreate AuditLog only if it doesn't already exist.
            //  The original Down used plain CreateTable, which would fail
            //  if some other path had already restored the table.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID('AuditLog', 'U') IS NULL
                BEGIN
                    CREATE TABLE [AuditLog] (
                        [Id]          uniqueidentifier   NOT NULL,
                        [UserId]      uniqueidentifier   NULL,
                        [Action]      nvarchar(80)       NOT NULL,
                        [AfterValue]  text               NULL,
                        [BeforeValue] text               NULL,
                        [EntityId]    uniqueidentifier   NOT NULL,
                        [EntityType]  nvarchar(50)       NOT NULL,
                        [IpAddress]   nvarchar(256)      NULL,
                        [Reason]      nvarchar(max)      NULL,
                        [TimeStamp]   datetime           NOT NULL,
                        [Username]    nvarchar(50)       NULL,
                        CONSTRAINT [PK_AuditLog] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_AuditLog_User] FOREIGN KEY ([UserId])
                            REFERENCES [User] ([Id])
                    );

                    CREATE INDEX [IX_AuditLog_EntityType_EntityId]
                        ON [AuditLog] ([EntityType], [EntityId]);

                    CREATE INDEX [IX_AuditLog_TimeStamp]
                        ON [AuditLog] ([TimeStamp]);

                    CREATE INDEX [IX_AuditLog_UserId]
                        ON [AuditLog] ([UserId]);

                    CREATE INDEX [IX_AuditLog_UserId_TimeStamp]
                        ON [AuditLog] ([UserId], [TimeStamp]);
                END
            ");
        }
    }
}