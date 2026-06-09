using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAppliesToAllFlagToDiscountCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- تغییرات AthleteQuestions ---
            migrationBuilder.DropColumn(name: "YourCity", table: "AthleteQuestions");
            migrationBuilder.DropColumn(name: "YourJob", table: "AthleteQuestions");
            migrationBuilder.AddColumn<string>(name: "NotesForCoach", table: "AthleteQuestions", type: "varchar(1000)", maxLength: 1000, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");


            migrationBuilder.AddColumn<bool>(
                name: "AppliesToAllServices",
                table: "DiscountCodes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false); // مقدار پیش‌فرض false است

            // 2. ستون جدید را بر اساس داده‌های قدیمی آپدیت می‌کنیم
            // اگر لیست سرویس‌ها خالی ('[]') یا null بود، یعنی برای همه سرویس‌هاست
            migrationBuilder.Sql(@"
                UPDATE DiscountCodes
                SET AppliesToAllServices = TRUE
                WHERE CoachServicesId IS NULL OR CoachServicesId = '[]' OR CoachServicesId = '';
            ");

            migrationBuilder.CreateTable(
                name: "DiscountCodeCoachServices",
                columns: table => new
                {
                    DiscountCodeId = table.Column<int>(type: "int", nullable: false),
                    CoachServiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodeCoachServices", x => new { x.DiscountCodeId, x.CoachServiceId });
                    table.ForeignKey(name: "FK_DiscountCodeCoachServices_CoachServices_CoachServiceId", column: x => x.CoachServiceId, principalTable: "CoachServices", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_DiscountCodeCoachServices_DiscountCodes_DiscountCodeId", column: x => x.DiscountCodeId, principalTable: "DiscountCodes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                INSERT INTO DiscountCodeCoachServices (DiscountCodeId, CoachServiceId)
                SELECT
                    d.Id AS DiscountCodeId,
                    jt.ServiceId AS CoachServiceId
                FROM
                    DiscountCodes AS d,
                    JSON_TABLE(
                        d.CoachServicesId,
                        '$[*]' COLUMNS (ServiceId INT PATH '$')
                    ) AS jt
                WHERE
                    JSON_VALID(d.CoachServicesId) AND d.CoachServicesId != '[]';
            ");
            
            migrationBuilder.DropColumn(
                name: "CoachServicesId",
                table: "DiscountCodes");

            migrationBuilder.CreateIndex(name: "IX_DiscountCodeCoachServices_CoachServiceId", table: "DiscountCodeCoachServices", column: "CoachServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // منطق Down هم باید کاملا برعکس عمل کند

            // 1. ستون قدیمی را اضافه می‌کنیم
            migrationBuilder.AddColumn<string>(name: "CoachServicesId", table: "DiscountCodes", type: "longtext", nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // 2. داده‌ها را از جدول واسط به ستون قدیمی برمی‌گردانیم (برای لیست‌های پر)
            migrationBuilder.Sql(@"
                UPDATE DiscountCodes AS dc
                JOIN (
                    SELECT DiscountCodeId, JSON_ARRAYAGG(CoachServiceId) AS ServicesJson
                    FROM DiscountCodeCoachServices
                    GROUP BY DiscountCodeId
                ) AS grouped_services ON dc.Id = grouped_services.DiscountCodeId
                SET dc.CoachServicesId = grouped_services.ServicesJson;
            ");

            // 3. برای مواردی که برای همه سرویس‌ها بوده، لیست خالی قرار می‌دهیم
            migrationBuilder.Sql(@"
                UPDATE DiscountCodes
                SET CoachServicesId = '[]'
                WHERE AppliesToAllServices = TRUE;
            ");

            // 4. جدول واسط و ستون جدید را حذف می‌کنیم
            migrationBuilder.DropTable(name: "DiscountCodeCoachServices");
            migrationBuilder.DropColumn(name: "AppliesToAllServices", table: "DiscountCodes");

            // 5. تغییرات AthleteQuestions را برمی‌گردانیم
            migrationBuilder.DropColumn(name: "NotesForCoach", table: "AthleteQuestions");
            migrationBuilder.AddColumn<string>(name: "YourCity", table: "AthleteQuestions", type: "longtext", nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "YourJob", table: "AthleteQuestions", type: "longtext", nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
