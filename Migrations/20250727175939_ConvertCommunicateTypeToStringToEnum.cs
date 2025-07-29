using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCommunicateTypeToStringToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommunicateType_Temp",
                table: "CoachServices",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE CoachServices SET CommunicateType_Temp = 0 WHERE CommunicateType = 'TELEGRAM';" +
                "UPDATE CoachServices SET CommunicateType_Temp = 1 WHERE CommunicateType = 'VIDEO_CALL';" +
                "UPDATE CoachServices SET CommunicateType_Temp = 2 WHERE CommunicateType = 'WHATSAPP';" +
                "UPDATE CoachServices SET CommunicateType_Temp = 3 WHERE CommunicateType = 'VOICE_CALL';"

            );

            // قدم ۳: ستون رشته‌ای قدیمی را حذف می‌کنیم
            migrationBuilder.DropColumn(
                name: "CommunicateType",
                table: "CoachServices");

            migrationBuilder.Sql(
                "ALTER TABLE CoachServices CHANGE COLUMN CommunicateType_Temp CommunicateType int NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // این بخش برای زمانی است که بخواهید مایگریشن را به عقب برگردانید (Rollback)

            // یک ستون موقت رشته‌ای می‌سازیم
            migrationBuilder.AddColumn<string>(
                name: "CommunicateType_Temp",
                table: "CoachServices",
                nullable: true);

            // داده‌های عددی را به رشته‌های معادل برمی‌گردانیم
            migrationBuilder.Sql(
                "UPDATE CoachServices SET CommunicateType_Temp = 'TELEGRAM' WHERE CommunicateType = 0;" +
                "UPDATE CoachServices SET CommunicateType_Temp = 'VIDEO_CALL' WHERE CommunicateType = 1;" +
                "UPDATE CoachServices SET CommunicateType_Temp = 'WHATSAPP' WHERE CommunicateType = 2;"+
                "UPDATE CoachServices SET CommunicateType_Temp = 'VOICE_CALL' WHERE CommunicateType = 3;"

            );

            // ستون عددی فعلی را حذف می‌کنیم
            migrationBuilder.DropColumn(
                name: "CommunicateType",
                table: "CoachServices");

            migrationBuilder.Sql(
                "ALTER TABLE CoachServices CHANGE COLUMN CommunicateType_Temp CommunicateType longtext NULL;");

        }
    }
}