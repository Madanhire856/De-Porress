using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateNominationPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanNominatePrefects",
                table: "UserGroup",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Bit 59 previously represented both nomination and cash-up. Preserve the legacy
            // academic capability for groups used by academic staff, without granting it to bursar-only groups.
            migrationBuilder.Sql("""
                UPDATE g SET CanNominatePrefects = 1
                FROM UserGroup g
                WHERE (COALESCE(g.RightsId, 0) & CAST(576460752303423488 AS bigint)) <> 0
                  AND EXISTS (SELECT 1 FROM [User] u WHERE u.GroupId = g.Id AND (u.RoleId & 103) <> 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanNominatePrefects",
                table: "UserGroup");
        }
    }
}
