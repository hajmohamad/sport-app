using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    public partial class ConvertProgramLevelToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgramLevel_Temp",
                table: "WorkoutPrograms",
                nullable: false,
                defaultValue: 0);

      
            migrationBuilder.Sql(
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 0 WHERE ProgramLevel = 'Beginner';" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 1 WHERE ProgramLevel = 'Intermediate';" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 2 WHERE ProgramLevel = 'SemiAdvanced';" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 3 WHERE ProgramLevel = 'Advanced';" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 4 WHERE ProgramLevel = 'UltraAdvanced';"
            );

            migrationBuilder.DropColumn(
                name: "ProgramLevel",
                table: "WorkoutPrograms");

          
            migrationBuilder.Sql(
                "ALTER TABLE WorkoutPrograms CHANGE COLUMN ProgramLevel_Temp ProgramLevel int NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // این بخش برای زمانی است که بخواهید مایگریشن را به عقب برگردانید (Rollback)

            // یک ستون موقت رشته‌ای می‌سازیم
            migrationBuilder.AddColumn<string>(
                name: "ProgramLevel_Temp",
                table: "WorkoutPrograms",
                type: "longtext",
                nullable: true);

            // داده‌های عددی را به رشته‌های معادل برمی‌گردانیم
            migrationBuilder.Sql(
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 'Beginner' WHERE ProgramLevel = 0;" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 'Intermediate' WHERE ProgramLevel = 1;" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 'SemiAdvanced' WHERE ProgramLevel = 2;" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 'Advanced' WHERE ProgramLevel = 3;" +
                "UPDATE WorkoutPrograms SET ProgramLevel_Temp = 'UltraAdvanced' WHERE ProgramLevel = 4;"
            );

            // ستون عددی فعلی را حذف می‌کنیم
            migrationBuilder.DropColumn(
                name: "ProgramLevel",
                table: "WorkoutPrograms");

            // نام ستون موقت را به نام اصلی تغییر می‌دهیم
            migrationBuilder.Sql(
                "ALTER TABLE WorkoutPrograms CHANGE COLUMN ProgramLevel_Temp ProgramLevel longtext NULL;");
        }
    }
}