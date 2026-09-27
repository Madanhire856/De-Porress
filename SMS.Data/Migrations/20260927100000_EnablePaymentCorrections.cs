using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    [DbContext(typeof(SMSDbContext))]
    [Migration("20260927100000_EnablePaymentCorrections")]
    public partial class EnablePaymentCorrections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectionReason",
                table: "Payment",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentCorrection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrectedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrectedByName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorrectedOn = table.Column<DateTime>(type: "datetime", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CorrectedAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PreviousBaseAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CorrectedBaseAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PreviousPaymentDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CorrectedPaymentDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    PreviousPaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    CorrectedPaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    PreviousReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CorrectedReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCorrection", x => x.Id);
                    table.ForeignKey("FK_PaymentCorrection_Payment", x => x.PaymentId, "Payment", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PaymentCorrection_User", x => x.CorrectedById, "User", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_PaymentCorrection_PaymentId", table: "PaymentCorrection", column: "PaymentId");
            migrationBuilder.CreateIndex(name: "IX_PaymentCorrection_CorrectedById", table: "PaymentCorrection", column: "CorrectedById");
            migrationBuilder.CreateIndex(name: "IX_PaymentCorrection_CorrectedOn", table: "PaymentCorrection", column: "CorrectedOn");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                    DROP TRIGGER dbo.TR_Payments_Immutable;
                IF OBJECT_ID(N'dbo.TR_AuditLog_Immutable', N'TR') IS NOT NULL
                    DROP TRIGGER dbo.TR_AuditLog_Immutable;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PaymentCorrection");
            migrationBuilder.DropColumn(name: "CorrectionReason", table: "Payment");
        }
    }
}
