using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class edit_some_classe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfRepeats",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "DescriptionOfPlan",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DurationByDay",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TitleOfPlan",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DurationByDay",
                table: "CoachesPlan");

            migrationBuilder.RenameColumn(
                name: "TypeOfCoachingPlan",
                table: "Payments",
                newName: "CoachPlanId");

            migrationBuilder.AddColumn<string>(
                name: "ExerciseLevel",
                table: "Exercises",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CommunicateType",
                table: "CoachesPlan",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "HaveSupport",
                table: "CoachesPlan",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CoachesPlan",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Activities",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CoachPlanId",
                table: "Payments",
                column: "CoachPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_CoachesPlan_CoachPlanId",
                table: "Payments",
                column: "CoachPlanId",
                principalTable: "CoachesPlan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_CoachesPlan_CoachPlanId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CoachPlanId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExerciseLevel",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CommunicateType",
                table: "CoachesPlan");

            migrationBuilder.DropColumn(
                name: "HaveSupport",
                table: "CoachesPlan");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CoachesPlan");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "CoachPlanId",
                table: "Payments",
                newName: "TypeOfCoachingPlan");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfRepeats",
                table: "WorkoutPrograms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionOfPlan",
                table: "Payments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DurationByDay",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TitleOfPlan",
                table: "Payments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DurationByDay",
                table: "CoachesPlan",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
