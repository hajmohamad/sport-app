using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class addworkoutprogramobjecttofeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkoutProgramFeedbackId",
                table: "WorkoutPrograms");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgramFeedback_WorkoutProgramId",
                table: "WorkoutProgramFeedback",
                column: "WorkoutProgramId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutProgramFeedback_WorkoutPrograms_WorkoutProgramId",
                table: "WorkoutProgramFeedback",
                column: "WorkoutProgramId",
                principalTable: "WorkoutPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutProgramFeedback_WorkoutPrograms_WorkoutProgramId",
                table: "WorkoutProgramFeedback");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutProgramFeedback_WorkoutProgramId",
                table: "WorkoutProgramFeedback");

            migrationBuilder.AddColumn<int>(
                name: "WorkoutProgramFeedbackId",
                table: "WorkoutPrograms",
                type: "int",
                nullable: true);
        }
    }
}
