using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class FixCoachIdSpelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CouchId",
                table: "WorkoutProgramFeedback",
                newName: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgramFeedback_CoachId",
                table: "WorkoutProgramFeedback",
                column: "CoachId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutProgramFeedback_Coaches_CoachId",
                table: "WorkoutProgramFeedback",
                column: "CoachId",
                principalTable: "Coaches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutProgramFeedback_Coaches_CoachId",
                table: "WorkoutProgramFeedback");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutProgramFeedback_CoachId",
                table: "WorkoutProgramFeedback");

            migrationBuilder.RenameColumn(
                name: "CoachId",
                table: "WorkoutProgramFeedback",
                newName: "CouchId");
        }
    }
}
