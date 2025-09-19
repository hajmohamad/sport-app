using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sport_app_backend.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AthleteFaq",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Question = table.Column<string>(type: "TEXT", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteFaq", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoachFaq",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Question = table.Column<string>(type: "TEXT", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachFaq", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeVerifies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    TimeCodeSend = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeVerifies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnglishName = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ImageLink = table.Column<string>(type: "TEXT", maxLength: 170, nullable: false),
                    VideoLink = table.Column<string>(type: "TEXT", maxLength: 170, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 3000, nullable: false),
                    Met = table.Column<double>(type: "REAL", nullable: false),
                    ExerciseLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetMuscles = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BaseMuscle = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    Mechanics = table.Column<int>(type: "INTEGER", nullable: false),
                    ForceType = table.Column<int>(type: "INTEGER", nullable: false),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseType = table.Column<int>(type: "INTEGER", nullable: false),
                    Equipment = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "date", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "date", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageProfile = table.Column<string>(type: "TEXT", nullable: false),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    TypeOfUser = table.Column<int>(type: "INTEGER", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Athletes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentWeight = table.Column<double>(type: "REAL", nullable: false),
                    WeightGoal = table.Column<double>(type: "REAL", nullable: false),
                    TimeBeforeWorkout = table.Column<int>(type: "INTEGER", nullable: false),
                    RestTime = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveWorkoutProgramId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Athletes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Disciplines = table.Column<string>(type: "TEXT", nullable: false),
                    Motivations = table.Column<string>(type: "TEXT", nullable: false),
                    WorkOnlineWithAthletes = table.Column<bool>(type: "INTEGER", nullable: false),
                    PresentsPracticeProgram = table.Column<string>(type: "TEXT", nullable: false),
                    TrackAthlete = table.Column<int>(type: "INTEGER", nullable: true),
                    ManagingRevenue = table.Column<bool>(type: "INTEGER", nullable: true),
                    DifficultTrackAthletes = table.Column<bool>(type: "INTEGER", nullable: false),
                    HardCommunicationWithAthletes = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachQuestions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportApps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportApps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportApps_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    CaloriesLost = table.Column<double>(type: "REAL", nullable: false),
                    Duration = table.Column<double>(type: "REAL", nullable: false),
                    Distance = table.Column<double>(type: "REAL", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AthleteQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "date", nullable: false),
                    FitnessLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentBodyForm = table.Column<int>(type: "INTEGER", nullable: false),
                    DaysPerWeekToExercise = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    ExerciseGoal = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AthleteQuestions_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChallengeType = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaterInDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOfCupsDrinked = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterInDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterInDays_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaterInTakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyCupOfWater = table.Column<int>(type: "INTEGER", nullable: false),
                    Reminder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterInTakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterInTakes_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    CurrentDate = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightEntries_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Coaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    CoachQuestionId = table.Column<int>(type: "INTEGER", nullable: true),
                    InstagramLink = table.Column<string>(type: "TEXT", maxLength: 51, nullable: false),
                    TelegramLink = table.Column<string>(type: "TEXT", maxLength: 51, nullable: false),
                    WhatsApp = table.Column<string>(type: "TEXT", maxLength: 51, nullable: false),
                    Verified = table.Column<bool>(type: "INTEGER", nullable: false),
                    HeadLine = table.Column<string>(type: "TEXT", maxLength: 124, nullable: false),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    ServiceFee = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coaches_CoachQuestions_CoachQuestionId",
                        column: x => x.CoachQuestionId,
                        principalTable: "CoachQuestions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Coaches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AthleteImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    AthleteQuestionId = table.Column<int>(type: "INTEGER", nullable: true),
                    FrontLink = table.Column<string>(type: "TEXT", nullable: true),
                    BackLink = table.Column<string>(type: "TEXT", nullable: true),
                    SideLink = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AthleteImage_AthleteQuestions_AthleteQuestionId",
                        column: x => x.AthleteQuestionId,
                        principalTable: "AthleteQuestions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InjuryAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteQuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    None = table.Column<bool>(type: "INTEGER", nullable: false),
                    Skeletal = table.Column<string>(type: "TEXT", nullable: true),
                    SoftTissueAndLigament = table.Column<string>(type: "TEXT", nullable: true),
                    InternalAndDigestive = table.Column<string>(type: "TEXT", nullable: true),
                    HormonalAndGlandular = table.Column<string>(type: "TEXT", nullable: true),
                    Specific = table.Column<string>(type: "TEXT", nullable: true),
                    Others = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InjuryAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InjuryAreas_AthleteQuestions_AthleteQuestionId",
                        column: x => x.AthleteQuestionId,
                        principalTable: "AthleteQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachPayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoachId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TransactionReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachPayouts_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoachId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    HaveSupport = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommunicateType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumberOfSell = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachServices_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CoachId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    CoachServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Authority = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AthleteQuestionId = table.Column<int>(type: "INTEGER", nullable: true),
                    AppFee = table.Column<double>(type: "REAL", nullable: false),
                    RefId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_AthleteQuestions_AthleteQuestionId",
                        column: x => x.AthleteQuestionId,
                        principalTable: "AthleteQuestions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Payments_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_CoachServices_CoachServiceId",
                        column: x => x.CoachServiceId,
                        principalTable: "CoachServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoachId = table.Column<int>(type: "INTEGER", nullable: false),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProgramDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramPriorities = table.Column<string>(type: "TEXT", nullable: false),
                    GeneralWarmUp = table.Column<string>(type: "TEXT", nullable: true),
                    DedicatedWarmUp = table.Column<int>(type: "INTEGER", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    LastExerciseDate = table.Column<DateTime>(type: "date", nullable: true),
                    TotalSessionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedSessionCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutPrograms_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutPrograms_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutPrograms_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramInDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkoutProgramId = table.Column<int>(type: "INTEGER", nullable: false),
                    ForWhichDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramInDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramInDays_WorkoutPrograms_WorkoutProgramId",
                        column: x => x.WorkoutProgramId,
                        principalTable: "WorkoutPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SingleExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProgramInDayId = table.Column<int>(type: "INTEGER", nullable: false),
                    Set = table.Column<int>(type: "INTEGER", nullable: false),
                    Rep = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingleExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SingleExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SingleExercises_ProgramInDays_ProgramInDayId",
                        column: x => x.ProgramInDayId,
                        principalTable: "ProgramInDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkoutProgramId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramInDayId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainingSessionStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseFeeling = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseCompletionBitmap = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_ProgramInDays_ProgramInDayId",
                        column: x => x.ProgramInDayId,
                        principalTable: "ProgramInDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_WorkoutPrograms_WorkoutProgramId",
                        column: x => x.WorkoutProgramId,
                        principalTable: "WorkoutPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SingleExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CoachId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainingSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseChangeRequests_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseChangeRequests_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseChangeRequests_SingleExercises_SingleExerciseId",
                        column: x => x.SingleExerciseId,
                        principalTable: "SingleExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseChangeRequests_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SingleExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPositive = table.Column<bool>(type: "INTEGER", nullable: false),
                    NegativeReason = table.Column<int>(type: "INTEGER", nullable: true),
                    AthleteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CoachId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainingSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_SingleExercises_SingleExerciseId",
                        column: x => x.SingleExerciseId,
                        principalTable: "SingleExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AthleteId",
                table: "Activities",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_AthleteImage_AthleteQuestionId",
                table: "AthleteImage",
                column: "AthleteQuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AthleteQuestions_AthleteId",
                table: "AthleteQuestions",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_UserId",
                table: "Athletes",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_AthleteId",
                table: "Challenges",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_Coaches_CoachQuestionId",
                table: "Coaches",
                column: "CoachQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Coaches_UserId",
                table: "Coaches",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachPayouts_CoachId",
                table: "CoachPayouts",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachQuestions_UserId",
                table: "CoachQuestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachServices_CoachId",
                table: "CoachServices",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChangeRequests_AthleteId",
                table: "ExerciseChangeRequests",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChangeRequests_CoachId",
                table: "ExerciseChangeRequests",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChangeRequests_SingleExerciseId",
                table: "ExerciseChangeRequests",
                column: "SingleExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChangeRequests_TrainingSessionId",
                table: "ExerciseChangeRequests",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_AthleteId",
                table: "ExerciseFeedbacks",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_CoachId",
                table: "ExerciseFeedbacks",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_SingleExerciseId",
                table: "ExerciseFeedbacks",
                column: "SingleExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_TrainingSessionId",
                table: "ExerciseFeedbacks",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InjuryAreas_AthleteQuestionId",
                table: "InjuryAreas",
                column: "AthleteQuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AthleteId",
                table: "Payments",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AthleteQuestionId",
                table: "Payments",
                column: "AthleteQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CoachId",
                table: "Payments",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CoachServiceId",
                table: "Payments",
                column: "CoachServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramInDays_WorkoutProgramId",
                table: "ProgramInDays",
                column: "WorkoutProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportApps_UserId",
                table: "ReportApps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SingleExercises_ExerciseId",
                table: "SingleExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_SingleExercises_ProgramInDayId",
                table: "SingleExercises",
                column: "ProgramInDayId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_ProgramInDayId",
                table: "TrainingSessions",
                column: "ProgramInDayId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_WorkoutProgramId",
                table: "TrainingSessions",
                column: "WorkoutProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterInDays_AthleteId",
                table: "WaterInDays",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterInTakes_AthleteId",
                table: "WaterInTakes",
                column: "AthleteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightEntries_AthleteId",
                table: "WeightEntries",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPrograms_AthleteId",
                table: "WorkoutPrograms",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPrograms_CoachId",
                table: "WorkoutPrograms",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPrograms_PaymentId",
                table: "WorkoutPrograms",
                column: "PaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "AthleteFaq");

            migrationBuilder.DropTable(
                name: "AthleteImage");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "CoachFaq");

            migrationBuilder.DropTable(
                name: "CoachPayouts");

            migrationBuilder.DropTable(
                name: "CodeVerifies");

            migrationBuilder.DropTable(
                name: "ExerciseChangeRequests");

            migrationBuilder.DropTable(
                name: "ExerciseFeedbacks");

            migrationBuilder.DropTable(
                name: "InjuryAreas");

            migrationBuilder.DropTable(
                name: "ReportApps");

            migrationBuilder.DropTable(
                name: "WaterInDays");

            migrationBuilder.DropTable(
                name: "WaterInTakes");

            migrationBuilder.DropTable(
                name: "WeightEntries");

            migrationBuilder.DropTable(
                name: "SingleExercises");

            migrationBuilder.DropTable(
                name: "TrainingSessions");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "ProgramInDays");

            migrationBuilder.DropTable(
                name: "WorkoutPrograms");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "AthleteQuestions");

            migrationBuilder.DropTable(
                name: "CoachServices");

            migrationBuilder.DropTable(
                name: "Athletes");

            migrationBuilder.DropTable(
                name: "Coaches");

            migrationBuilder.DropTable(
                name: "CoachQuestions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
