using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeUserIndexesAndForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Athletes_Users_UserId",
                table: "Athletes");

            migrationBuilder.DropForeignKey(
                name: "FK_Coaches_Users_UserId",
                table: "Coaches");

            migrationBuilder.DropIndex(
                name: "IX_Coaches_UserId",
                table: "Coaches");

            migrationBuilder.DropIndex(
                name: "IX_Athletes_UserId",
                table: "Athletes");

            migrationBuilder.AlterColumn<string>(
                name: "SiteRefreshToken",
                table: "Users",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AthleteId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoachId",
                table: "Users",
                type: "int",
                nullable: true);
            migrationBuilder.Sql("UPDATE Users U INNER JOIN Athletes A ON U.Id = A.UserId SET U.AthleteId = A.Id");
            migrationBuilder.Sql("UPDATE Users U INNER JOIN Coaches C ON U.Id = C.UserId SET U.CoachId = C.Id");


            migrationBuilder.CreateIndex(
                name: "IX_User_SiteRefreshToken",
                table: "Users",
                column: "SiteRefreshToken");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AthleteId",
                table: "Users",
                column: "AthleteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CoachId",
                table: "Users",
                column: "CoachId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Athletes_AthleteId",
                table: "Users",
                column: "AthleteId",
                principalTable: "Athletes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Coaches_CoachId",
                table: "Users",
                column: "CoachId",
                principalTable: "Coaches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Athletes_AthleteId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Coaches_CoachId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_User_SiteRefreshToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AthleteId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CoachId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AthleteId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CoachId",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "SiteRefreshToken",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Coaches_UserId",
                table: "Coaches",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_UserId",
                table: "Athletes",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Athletes_Users_UserId",
                table: "Athletes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Coaches_Users_UserId",
                table: "Coaches",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
