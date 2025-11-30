using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class addNewQuestionsForAthlete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComingCompetition",
                table: "AthleteQuestions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CompetitionHistory",
                table: "AthleteQuestions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CurrentMedications",
                table: "AthleteQuestions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SittingHour",
                table: "AthleteQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "YourCity",
                table: "AthleteQuestions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "YourJob",
                table: "AthleteQuestions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComingCompetition",
                table: "AthleteQuestions");

            migrationBuilder.DropColumn(
                name: "CompetitionHistory",
                table: "AthleteQuestions");

            migrationBuilder.DropColumn(
                name: "CurrentMedications",
                table: "AthleteQuestions");

            migrationBuilder.DropColumn(
                name: "SittingHour",
                table: "AthleteQuestions");

            migrationBuilder.DropColumn(
                name: "YourCity",
                table: "AthleteQuestions");

            migrationBuilder.DropColumn(
                name: "YourJob",
                table: "AthleteQuestions");
        }
    }
}
