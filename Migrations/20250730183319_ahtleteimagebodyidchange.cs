using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class ahtleteimagebodyidchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AthleteImage_AthleteQuestions_AthleteQuestionId",
                table: "AthleteImage");

            migrationBuilder.AlterColumn<int>(
                name: "AthleteQuestionId",
                table: "AthleteImage",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AthleteImage_AthleteQuestions_AthleteQuestionId",
                table: "AthleteImage",
                column: "AthleteQuestionId",
                principalTable: "AthleteQuestions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AthleteImage_AthleteQuestions_AthleteQuestionId",
                table: "AthleteImage");

            migrationBuilder.AlterColumn<int>(
                name: "AthleteQuestionId",
                table: "AthleteImage",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AthleteImage_AthleteQuestions_AthleteQuestionId",
                table: "AthleteImage",
                column: "AthleteQuestionId",
                principalTable: "AthleteQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
