using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class mysqlAddSiteToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DedicatedWarmUp",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "GeneralWarmUp",
                table: "WorkoutPrograms");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginSite",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SiteRefreshToken",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "SupportApp",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SupportApp",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLoginSite",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SiteRefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SupportApp");

            migrationBuilder.AddColumn<int>(
                name: "DedicatedWarmUp",
                table: "WorkoutPrograms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneralWarmUp",
                table: "WorkoutPrograms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "SupportApp",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
