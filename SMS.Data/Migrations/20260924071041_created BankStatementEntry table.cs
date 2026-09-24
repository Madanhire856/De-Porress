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
            migrationBuilder.DropForeignKey(
                name: "FK_BankStatementEntry_User",
                table: "BankStatementEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_BankStatementEntry_User1",
                table: "BankStatementEntry");

            migrationBuilder.AlterColumn<Guid>(
                name: "MatchedByUserId",
                table: "BankStatementEntry",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsMatched",
                table: "BankStatementEntry",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EntryDate",
                table: "BankStatementEntry",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "json",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "json",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatementEntry_User",
                table: "BankStatementEntry",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatementEntry_User1",
                table: "BankStatementEntry",
                column: "MatchedByUserId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankStatementEntry_User",
                table: "BankStatementEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_BankStatementEntry_User1",
                table: "BankStatementEntry");

            migrationBuilder.AlterColumn<Guid>(
                name: "MatchedByUserId",
                table: "BankStatementEntry",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<bool>(
                name: "IsMatched",
                table: "BankStatementEntry",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EntryDate",
                table: "BankStatementEntry",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BeforeValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AfterValue",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatementEntry_User",
                table: "BankStatementEntry",
                column: "MatchedByUserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatementEntry_User1",
                table: "BankStatementEntry",
                column: "CreatorId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
