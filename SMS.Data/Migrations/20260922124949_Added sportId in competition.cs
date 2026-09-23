using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedsportIdincompetition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_House_Staff",
                table: "House");

            migrationBuilder.DropForeignKey(
                name: "FK_Sport_Staff",
                table: "Sport");

            migrationBuilder.DropIndex(
                name: "IX_Sport_MasterId",
                table: "Sport");

            migrationBuilder.DropIndex(
                name: "IX_House_MasterId",
                table: "House");

            migrationBuilder.DropColumn(
                name: "MasterId",
                table: "Sport");

            migrationBuilder.DropColumn(
                name: "MasterId",
                table: "House");

            migrationBuilder.AddColumn<Guid>(
                name: "SportId",
                table: "Competition",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "HouseTeacher",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseTeacher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseTeacher_House",
                        column: x => x.HouseId,
                        principalTable: "House",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HouseTeacher_Staff",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HouseTeacher_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SportTeacher",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportTeacher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SportTeacher_Sport",
                        column: x => x.SportId,
                        principalTable: "Sport",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SportTeacher_Staff",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SportTeacher_User",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Competition_SportId",
                table: "Competition",
                column: "SportId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseTeacher_CreatorId",
                table: "HouseTeacher",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseTeacher_HouseId",
                table: "HouseTeacher",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseTeacher_StaffId",
                table: "HouseTeacher",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SportTeacher_CreatorId",
                table: "SportTeacher",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SportTeacher_SportId",
                table: "SportTeacher",
                column: "SportId");

            migrationBuilder.CreateIndex(
                name: "IX_SportTeacher_StaffId",
                table: "SportTeacher",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Competition_Competition",
                table: "Competition",
                column: "SportId",
                principalTable: "Competition",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competition_Competition",
                table: "Competition");

            migrationBuilder.DropTable(
                name: "HouseTeacher");

            migrationBuilder.DropTable(
                name: "SportTeacher");

            migrationBuilder.DropIndex(
                name: "IX_Competition_SportId",
                table: "Competition");

            migrationBuilder.DropColumn(
                name: "SportId",
                table: "Competition");

            migrationBuilder.AddColumn<Guid>(
                name: "MasterId",
                table: "Sport",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MasterId",
                table: "House",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Sport_MasterId",
                table: "Sport",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_House_MasterId",
                table: "House",
                column: "MasterId");

            migrationBuilder.AddForeignKey(
                name: "FK_House_Staff",
                table: "House",
                column: "MasterId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sport_Staff",
                table: "Sport",
                column: "MasterId",
                principalTable: "Staff",
                principalColumn: "Id");
        }
    }
}
