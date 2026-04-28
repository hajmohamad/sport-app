using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class FixLastWorkoutExerciseRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LastWorkoutExercises_Coaches_AthleteId",
                table: "LastWorkoutExercises");

            migrationBuilder.AddForeignKey(
                name: "FK_LastWorkoutExercises_Athletes_AthleteId",
                table: "LastWorkoutExercises",
                column: "AthleteId",
                principalTable: "Athletes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LastWorkoutExercises_Athletes_AthleteId",
                table: "LastWorkoutExercises");

            migrationBuilder.AddForeignKey(
                name: "FK_LastWorkoutExercises_Coaches_AthleteId",
                table: "LastWorkoutExercises",
                column: "AthleteId",
                principalTable: "Coaches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
