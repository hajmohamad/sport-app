using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class removePublicDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
