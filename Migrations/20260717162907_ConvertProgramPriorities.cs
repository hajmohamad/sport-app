using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class ConvertProgramPriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProgramPriorities",
                table: "WorkoutProgramTemplates",
                newName: "ProgramPriority");

            migrationBuilder.RenameColumn(
                name: "ProgramPriorities",
                table: "WorkoutPrograms",
                newName: "ProgramPriority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProgramPriority",
                table: "WorkoutProgramTemplates",
                newName: "ProgramPriorities");

            migrationBuilder.RenameColumn(
                name: "ProgramPriority",
                table: "WorkoutPrograms",
                newName: "ProgramPriorities");
        }
    }
}
