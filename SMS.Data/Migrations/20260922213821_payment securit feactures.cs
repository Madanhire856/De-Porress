using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class paymentsecuritfeactures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethodId",
                table: "Payment",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatorId",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // CreationDate: datetime? → datetime NOT NULL
            // SQL Server refuses DateTime.MinValue as a default. Drop and re-add
            // with GETDATE() — safe because Payment is empty.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE object_id = OBJECT_ID('Payment')
                             AND name = 'CreationDate')
                BEGIN
                    ALTER TABLE [Payment] DROP COLUMN [CreationDate];
                END;

                ALTER TABLE [Payment] ADD [CreationDate] datetime NOT NULL
                    CONSTRAINT DF_Payment_CreationDate DEFAULT (GETDATE());
            ");

            migrationBuilder.AddColumn<int>(
                name: "BaseAmount",
                table: "Payment",
                type: "int",
                nullable: true);

            // CreatedByName: string? — added as nvarchar, not varbinary
            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Payment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Payment",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReversal",
                table: "Payment",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RateSource",
                table: "Payment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptNumber",
                table: "Payment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptYear",
                table: "Payment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversedById",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedOn",
                table: "Payment",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversesPaymentId",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: true);

            // ReceiptSequece table — kept in this migration so it matches history.
            // The corrective migration later drops it and creates ReceiptSequence.
            migrationBuilder.CreateTable(
                name: "ReceiptSequece",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    LastIssuedOn = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptSequece", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ReversedById",
                table: "Payment",
                column: "ReversedById");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ReversesPaymentId",
                table: "Payment",
                column: "ReversesPaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_Payment",
                table: "Payment",
                column: "ReversesPaymentId",
                principalTable: "Payment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_User1",
                table: "Payment",
                column: "ReversedById",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_Payment",
                table: "Payment");

            migrationBuilder.DropForeignKey(
                name: "FK_Payment_User1",
                table: "Payment");

            migrationBuilder.DropTable(
                name: "ReceiptSequece");

            migrationBuilder.DropIndex(
                name: "IX_Payment_ReversedById",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_ReversesPaymentId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "IsReversal",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "RateSource",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ReceiptYear",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ReversedById",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ReversedOn",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ReversesPaymentId",
                table: "Payment");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethodId",
                table: "Payment",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatorId",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // CreationDate: revert to nullable — drop and re-add to avoid type clash
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE object_id = OBJECT_ID('Payment')
                             AND name = 'CreationDate')
                BEGIN
                    ALTER TABLE [Payment] DROP COLUMN [CreationDate];
                END;

                ALTER TABLE [Payment] ADD [CreationDate] datetime NULL;
            ");
        }
    }
}