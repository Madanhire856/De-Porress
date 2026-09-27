using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class createdAuditlogtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────
            //  AuditLog was created out-of-band via raw SQL, so this
            //  migration is a no-op. It exists only to record the model
            //  state in __EFMigrationsHistory.
            //
            //  Guarded creation is included anyway so a clean rebuild of the
            //  database (where the SQL was not run manually) still works.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID('AuditLog', 'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[AuditLog]
                    (
                        [Id]          UNIQUEIDENTIFIER NOT NULL,
                        [UserId]      UNIQUEIDENTIFIER NULL,
                        [Action]      NVARCHAR(80)     NOT NULL,
                        [AfterValue]  NVARCHAR(MAX)    NULL,
                        [BeforeValue] NVARCHAR(MAX)    NULL,
                        [EntityId]    UNIQUEIDENTIFIER NOT NULL,
                        [EntityType]  NVARCHAR(50)     NOT NULL,
                        [IpAddress]   NVARCHAR(256)    NULL,
                        [Reason]      NVARCHAR(MAX)    NULL,
                        [TimeStamp]   DATETIME         NOT NULL,
                        [Username]    NVARCHAR(50)     NULL,
                        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id]),
                        CONSTRAINT [FK_AuditLog_User] FOREIGN KEY ([UserId])
                            REFERENCES [dbo].[User] ([Id])
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_AuditLog_EntityType_EntityId'
                                 AND object_id = OBJECT_ID('dbo.AuditLog'))
                    CREATE INDEX [IX_AuditLog_EntityType_EntityId]
                        ON [dbo].[AuditLog] ([EntityType], [EntityId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_AuditLog_TimeStamp'
                                 AND object_id = OBJECT_ID('dbo.AuditLog'))
                    CREATE INDEX [IX_AuditLog_TimeStamp]
                        ON [dbo].[AuditLog] ([TimeStamp]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_AuditLog_UserId'
                                 AND object_id = OBJECT_ID('dbo.AuditLog'))
                    CREATE INDEX [IX_AuditLog_UserId]
                        ON [dbo].[AuditLog] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_AuditLog_UserId_TimeStamp'
                                 AND object_id = OBJECT_ID('dbo.AuditLog'))
                    CREATE INDEX [IX_AuditLog_UserId_TimeStamp]
                        ON [dbo].[AuditLog] ([UserId], [TimeStamp]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. AuditLog is not being dropped by this
            // migration's reversal.
        }
    }
}