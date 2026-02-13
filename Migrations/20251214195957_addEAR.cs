using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class addEAR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HaveSupport",
                table: "CoachServices");

            migrationBuilder.AddColumn<int>(
                name: "ActivityLevel",
                table: "AthleteQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "AthleteQuestions");

            migrationBuilder.AddColumn<bool>(
                name: "HaveSupport",
                table: "CoachServices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
