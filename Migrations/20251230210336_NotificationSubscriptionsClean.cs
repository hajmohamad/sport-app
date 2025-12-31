using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class NotificationSubscriptionsClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ActiveWorkoutProgramId",
                table: "Athletes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
            migrationBuilder.Sql(@"
                    UPDATE Athletes a
                    LEFT JOIN (
                        SELECT wp.AthleteId,
                               COALESCE(
                                   MAX(CASE WHEN wp.Status = 2 THEN wp.Id END),
                                   MAX(CASE WHEN wp.Status = 3 THEN wp.Id END)
                               ) AS ActiveProgramId
                        FROM WorkoutPrograms wp
                        GROUP BY wp.AthleteId
                    ) w ON a.Id = w.AthleteId
                    SET a.ActiveWorkoutProgramId = w.ActiveProgramId;
                ");

            migrationBuilder.CreateTable(
                name: "NotificationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Endpoint = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    P256DH = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Auth = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastTrainingReminderSentAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_ActiveWorkoutProgramId",
                table: "Athletes",
                column: "ActiveWorkoutProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSubscriptions_UserId",
                table: "NotificationSubscriptions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Athletes_WorkoutPrograms_ActiveWorkoutProgramId",
                table: "Athletes",
                column: "ActiveWorkoutProgramId",
                principalTable: "WorkoutPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Athletes_WorkoutPrograms_ActiveWorkoutProgramId",
                table: "Athletes");

            migrationBuilder.DropTable(
                name: "NotificationSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Athletes_ActiveWorkoutProgramId",
                table: "Athletes");

            migrationBuilder.AlterColumn<int>(
                name: "ActiveWorkoutProgramId",
                table: "Athletes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
