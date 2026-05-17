using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class editpinedMessageAndLastExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouchId",
                table: "LastWorkoutExercises");

            migrationBuilder.DropColumn(
                name: "CouchId",
                table: "CoachPineExercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CouchId",
                table: "LastWorkoutExercises",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CouchId",
                table: "CoachPineExercises",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
