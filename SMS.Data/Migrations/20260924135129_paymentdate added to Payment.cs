using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class paymentdateaddedtoPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TR_Payments_Immutable', N'TR') IS NOT NULL
                    DROP TRIGGER dbo.TR_Payments_Immutable;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — nothing to undo.
        }
    }
}