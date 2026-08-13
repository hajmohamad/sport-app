using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class addcoachStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                    name: "Slogan",
                    table: "Coaches",
                    type: "varchar(40)",
                    maxLength: 40,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                    name: "SiteDescription",
                    table: "Coaches",
                    type: "varchar(400)",
                    maxLength: 400,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CoachStatus",
                table: "Coaches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 0: ShowWebsite=false و WebSiteUrl خالی
            // 1: ShowWebsite=false و WebSiteUrl پر
            // 2: ShowWebsite=true و Verified=false
            // 3: Verified=true
            migrationBuilder.Sql("""
                                 UPDATE Coaches
                                 SET CoachStatus = CASE
                                     WHEN Verified = 1 THEN 3
                                     WHEN ShowWebsite = 1 THEN 2
                                     WHEN WebSiteUrl IS NOT NULL AND TRIM(WebSiteUrl) <> '' THEN 1
                                     ELSE 0
                                 END;
                                 """);

            migrationBuilder.DropColumn(
                name: "ShowWebsite",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "Verified",
                table: "Coaches");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoachStatus",
                table: "Coaches");

            migrationBuilder.AlterColumn<string>(
                name: "Slogan",
                table: "Coaches",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "SiteDescription",
                table: "Coaches",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldMaxLength: 250)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "ShowWebsite",
                table: "Coaches",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Verified",
                table: "Coaches",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
