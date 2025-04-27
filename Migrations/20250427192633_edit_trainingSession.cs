using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class edit_trainingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkoutProgramId",
                table: "TrainingSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_WorkoutProgramId",
                table: "TrainingSessions",
                column: "WorkoutProgramId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingSessions_WorkoutPrograms_WorkoutProgramId",
                table: "TrainingSessions",
                column: "WorkoutProgramId",
                principalTable: "WorkoutPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingSessions_WorkoutPrograms_WorkoutProgramId",
                table: "TrainingSessions");

            migrationBuilder.DropIndex(
                name: "IX_TrainingSessions_WorkoutProgramId",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "WorkoutProgramId",
                table: "TrainingSessions");
        }
    }
}
