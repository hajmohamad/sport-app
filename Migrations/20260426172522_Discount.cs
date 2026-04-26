using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class Discount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CodeDiscountAmount",
                table: "Payments",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountCodeId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OriginalAmount",
                table: "Payments",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PublicDiscountAmount",
                table: "Payments",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfSellWithDiscount",
                table: "CoachServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicDiscountExpiresAt",
                table: "CoachServices",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicDiscountPercent",
                table: "CoachServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimit",
                table: "CoachServices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CoachId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiscountPercent = table.Column<int>(type: "int", nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountCodes_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DiscountCodeId",
                table: "Payments",
                column: "DiscountCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_CoachId",
                table: "DiscountCodes",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_Code",
                table: "DiscountCodes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DiscountCodes_DiscountCodeId",
                table: "Payments",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DiscountCodes_DiscountCodeId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DiscountCodeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CodeDiscountAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DiscountCodeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PublicDiscountAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "NumberOfSellWithDiscount",
                table: "CoachServices");

            migrationBuilder.DropColumn(
                name: "PublicDiscountExpiresAt",
                table: "CoachServices");

            migrationBuilder.DropColumn(
                name: "PublicDiscountPercent",
                table: "CoachServices");

            migrationBuilder.DropColumn(
                name: "UsageLimit",
                table: "CoachServices");
        }
    }
}
