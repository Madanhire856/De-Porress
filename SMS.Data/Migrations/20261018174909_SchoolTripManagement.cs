using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class SchoolTripManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolTrip",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PlacesToPassThrough = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LeaderStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountPerStudent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    PaymentDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DepartureAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MeetingPoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TransportDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Accommodation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ParentConsentRequired = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolTrip", x => x.Id);
                    table.CheckConstraint("CK_SchoolTrip_Amount", "[AmountPerStudent] >= 0");
                    table.CheckConstraint("CK_SchoolTrip_Capacity", "[Capacity] IS NULL OR [Capacity] > 0");
                    table.CheckConstraint("CK_SchoolTrip_Dates", "[ReturnAt] > [DepartureAt]");
                    table.CheckConstraint("CK_SchoolTrip_DueDate", "([AmountPerStudent] = 0 OR [PaymentDueDate] IS NOT NULL) AND ([PaymentDueDate] IS NULL OR [PaymentDueDate] <= CAST([DepartureAt] AS date))");
                    table.CheckConstraint("CK_SchoolTrip_Status", "[StatusId] IN (0,1,2,3)");
                    table.ForeignKey(
                        name: "FK_SchoolTrip_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolTrip_Staff_LeaderStaffId",
                        column: x => x.LeaderStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolTrip_User_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolTrip_User_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolTripGrade",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolTripGrade", x => new { x.TripId, x.GradeId });
                    table.ForeignKey(
                        name: "FK_SchoolTripGrade_Grade_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolTripGrade_SchoolTrip_TripId",
                        column: x => x.TripId,
                        principalTable: "SchoolTrip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTrip_CreatorId",
                table: "SchoolTrip",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTrip_CurrencyCode",
                table: "SchoolTrip",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTrip_LeaderStaffId",
                table: "SchoolTrip",
                column: "LeaderStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTrip_StatusId_DepartureAt",
                table: "SchoolTrip",
                columns: new[] { "StatusId", "DepartureAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTrip_UpdatedById",
                table: "SchoolTrip",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTripGrade_GradeId",
                table: "SchoolTripGrade",
                column: "GradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolTripGrade");

            migrationBuilder.DropTable(
                name: "SchoolTrip");
        }
    }
}
