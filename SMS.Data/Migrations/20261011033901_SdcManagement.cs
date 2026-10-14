using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class SdcManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SdcPosition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdcPosition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SdcPosition_User_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SdcTerm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdcTerm", x => x.Id);
                    table.CheckConstraint("CK_SdcTerm_Dates", "[EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_SdcTerm_User_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SdcMember",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdcMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SdcMember_SdcPosition_PositionId",
                        column: x => x.PositionId,
                        principalTable: "SdcPosition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SdcMember_SdcTerm_TermId",
                        column: x => x.TermId,
                        principalTable: "SdcTerm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SdcMember_User_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SdcMember_CreatorId",
                table: "SdcMember",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SdcMember_PositionId",
                table: "SdcMember",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_SdcMember_TermId",
                table: "SdcMember",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_SdcPosition_CreatorId",
                table: "SdcPosition",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SdcPosition_Name",
                table: "SdcPosition",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SdcTerm_CreatorId",
                table: "SdcTerm",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SdcTerm_Name",
                table: "SdcTerm",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SdcMember");

            migrationBuilder.DropTable(
                name: "SdcPosition");

            migrationBuilder.DropTable(
                name: "SdcTerm");
        }
    }
}
