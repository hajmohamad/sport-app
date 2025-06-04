using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class activeworkotprogramId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveWorkoutProgramId",
                table: "Athletes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveWorkoutProgramId",
                table: "Athletes");
        }
    }
}
