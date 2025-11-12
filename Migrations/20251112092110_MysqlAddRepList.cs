using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class MysqlAddRepList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SingleExercises",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RepType",
                table: "SingleExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepsJson",
                table: "SingleExercises",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "SingleExercises");

            migrationBuilder.DropColumn(
                name: "RepType",
                table: "SingleExercises");

            migrationBuilder.DropColumn(
                name: "RepsJson",
                table: "SingleExercises");
        }
    }
}
