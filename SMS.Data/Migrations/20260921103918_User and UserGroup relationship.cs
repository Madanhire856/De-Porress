using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserandUserGrouprelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TwoFactorAuthEnabled: bit (non-null) → bit (nullable)
            // SQL Server allows this alter directly.
            migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorAuthEnabled",
                table: "User",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            // GroupId: int → uniqueidentifier
            // SQL Server refuses this conversion outright. Drop and re-add.
            // Safe because the DB is freshly built — no data to lose.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE object_id = OBJECT_ID('User')
                             AND name = 'GroupId')
                BEGIN
                    ALTER TABLE [User] DROP COLUMN [GroupId];
                END;

                ALTER TABLE [User] ADD [GroupId] uniqueidentifier NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_User_GroupId",
                table: "User",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_UserGroup",
                table: "User",
                column: "GroupId",
                principalTable: "UserGroup",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_UserGroup",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_GroupId",
                table: "User");

            migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorAuthEnabled",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            // GroupId: uniqueidentifier → int
            // Same pattern — drop and re-add on rollback too.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE object_id = OBJECT_ID('User')
                             AND name = 'GroupId')
                BEGIN
                    ALTER TABLE [User] DROP COLUMN [GroupId];
                END;

                ALTER TABLE [User] ADD [GroupId] int NULL;
            ");
        }
    }
}