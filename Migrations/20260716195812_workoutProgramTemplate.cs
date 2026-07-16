using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class workoutProgramTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProgramPriorities",
                table: "WorkoutPrograms",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkoutProgramTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CoachId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgramDuration = table.Column<int>(type: "int", nullable: false),
                    ProgramLevel = table.Column<int>(type: "int", nullable: false),
                    ProgramPriorities = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutProgramTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutProgramTemplates_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TemplateProgramInDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkoutProgramTemplateId = table.Column<int>(type: "int", nullable: false),
                    ForWhichDay = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateProgramInDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateProgramInDays_WorkoutProgramTemplates_WorkoutProgram~",
                        column: x => x.WorkoutProgramTemplateId,
                        principalTable: "WorkoutProgramTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TemplateSingleExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TemplateProgramInDayId = table.Column<int>(type: "int", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    RepType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RepsJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSingleExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateSingleExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateSingleExercises_TemplateProgramInDays_TemplateProgra~",
                        column: x => x.TemplateProgramInDayId,
                        principalTable: "TemplateProgramInDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateProgramInDays_WorkoutProgramTemplateId",
                table: "TemplateProgramInDays",
                column: "WorkoutProgramTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSingleExercises_ExerciseId",
                table: "TemplateSingleExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSingleExercises_TemplateProgramInDayId",
                table: "TemplateSingleExercises",
                column: "TemplateProgramInDayId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgramTemplates_CoachId",
                table: "WorkoutProgramTemplates",
                column: "CoachId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateSingleExercises");

            migrationBuilder.DropTable(
                name: "TemplateProgramInDays");

            migrationBuilder.DropTable(
                name: "WorkoutProgramTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "ProgramPriorities",
                table: "WorkoutPrograms",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
