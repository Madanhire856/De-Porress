using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Auditadjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1. UserId → nullable (was NOT NULL, system actions have no user) ----
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuditLog",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // ---- 2. Widen value columns: json → nvarchar(max) nullable ----
            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json");

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json");

            // ---- 3. New context columns ----
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLog",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "AuditLog",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // ---- 4. Indexes for the common lookups ----
            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId_TimeStamp",
                table: "AuditLog",
                columns: new[] { "UserId", "TimeStamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TimeStamp",
                table: "AuditLog",
                column: "TimeStamp");

            // ============================================================
            //  IMMUTABLE TRIGGER — blocks UPDATE and DELETE on AuditLog
            //  Drop first if it somehow already exists (idempotent).
            // ============================================================
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_AuditLog_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER TR_AuditLog_Immutable;
            ");

            // CREATE TRIGGER must be the first statement in a batch,
            // so wrap it in EXEC(...).
            migrationBuilder.Sql(@"
                EXEC('
                    CREATE TRIGGER TR_AuditLog_Immutable
                    ON AuditLog
                    AFTER UPDATE, DELETE
                    AS
                    BEGIN
                        RAISERROR(''AuditLog records are immutable and cannot be modified or deleted.'', 16, 1);
                        ROLLBACK TRANSACTION;
                    END;
                ');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ---- Drop the trigger FIRST — before any column changes ----
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_AuditLog_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER TR_AuditLog_Immutable;
            ");

            // ---- Drop indexes ----
            migrationBuilder.DropIndex(
                name: "IX_AuditLog_EntityType_EntityId",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_UserId_TimeStamp",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_TimeStamp",
                table: "AuditLog");

            // ---- Drop new columns ----
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "AuditLog");

            // ---- Revert value columns: nvarchar(max) → json NOT NULL ----
            // NOTE: This will FAIL if any existing rows have NULL in these
            // columns. If you're rolling back into a state where the DB has
            // such rows, run this first:
            //   UPDATE AuditLog SET BeforeValue = '{}' WHERE BeforeValue IS NULL;
            //   UPDATE AuditLog SET AfterValue  = '{}' WHERE AfterValue  IS NULL;
            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "json",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "json",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // ---- Revert UserId: nullable → NOT NULL ----
            // This will FAIL if any existing rows have NULL UserId.
            // If you're rolling back with system-generated rows present:
            //   DELETE FROM AuditLog WHERE UserId IS NULL;
            // Or:
            //   UPDATE AuditLog SET UserId = '<some-existing-user-guid>' WHERE UserId IS NULL;
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuditLog",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}