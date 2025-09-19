using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class editPaymentAthleteQeustion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AthleteQuestions_AthleteQuestionId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "AthleteQuestionId",
                table: "Payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AthleteQuestions_AthleteQuestionId",
                table: "Payments",
                column: "AthleteQuestionId",
                principalTable: "AthleteQuestions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AthleteQuestions_AthleteQuestionId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "AthleteQuestionId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AthleteQuestions_AthleteQuestionId",
                table: "Payments",
                column: "AthleteQuestionId",
                principalTable: "AthleteQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
