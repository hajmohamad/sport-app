using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedSessionCount",
                table: "WorkoutPrograms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastExerciseDate",
                table: "WorkoutPrograms",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSessionCount",
                table: "WorkoutPrograms",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedSessionCount",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "LastExerciseDate",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "TotalSessionCount",
                table: "WorkoutPrograms");
        }
    }
}
