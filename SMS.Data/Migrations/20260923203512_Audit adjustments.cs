using System;
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

            // ---- 2. Widen value columns: nvarchar(max) nullable (Replaced invalid "json" type) ----
            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

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

            // ---- 4. Indexes for common lookups ----
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

            // ---- 5. IMMUTABLE TRIGGER — blocks UPDATE and DELETE on AuditLog ----
            migrationBuilder.Sql(@"
                IF OBJECT_ID('TR_AuditLog_Immutable', 'TR') IS NOT NULL
                    DROP TRIGGER TR_AuditLog_Immutable;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER TR_AuditLog_Immutable
                ON AuditLog
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    RAISERROR('AuditLog records are immutable and cannot be modified or deleted.', 16, 1);
                    ROLLBACK TRANSACTION;
                END;
            ", suppressTransaction: true);
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

            // ---- Revert value columns to NOT NULL nvarchar(max) ----
            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // ---- Revert UserId: nullable → NOT NULL ----
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