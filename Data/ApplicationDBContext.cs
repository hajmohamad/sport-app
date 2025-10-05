using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Challenge_Achievement;
using sport_app_backend.Models.Login_Sinup;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.SupportApp;


namespace sport_app_backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions dbContextOptions)
        : base(dbContextOptions)
    { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
         modelBuilder.Entity<User>()
        .Navigation(u => u.Coach)
        .AutoInclude(); 
         

         modelBuilder.Entity<User>()
        .Navigation(u => u.Athlete)
        .AutoInclude(); 
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Coach> Coaches { get; set; }
    public DbSet<Athlete> Athletes { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutProgram> WorkoutPrograms { get; set; }
    public DbSet<ProgramInDay> ProgramInDays { get; set; }
    public DbSet<SingleExercise> SingleExercises { get; set; }
    public DbSet<CodeVerify> CodeVerifies { get; set; }
    public DbSet<CoachQuestion> CoachQuestions { get; set; }
    public DbSet<AthleteQuestion> AthleteQuestions { get; set; }
    public DbSet<WeightEntry> WeightEntries { get; set; }
    public DbSet<WaterInTake> WaterInTakes { get; set; }
    public DbSet<WaterInDay> WaterInDays { get; set; }
    public DbSet<CoachService> CoachServices { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<SupportApp> SupportApp { get; set; }
    public DbSet<InjuryArea> InjuryAreas { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<TrainingSession> TrainingSessions  { get; set; }
    public DbSet<ExerciseFeedback> ExerciseFeedbacks { get; set; }
    public DbSet<ExerciseChangeRequest> ExerciseChangeRequests { get; set; }
    public DbSet<CoachPayout> CoachPayouts { get; set; }
    public DbSet<CoachFaq> CoachFaq { get; set; }
    public DbSet<AthleteFaq> AthleteFaq { get; set; }
    public DbSet<AthleteBodyImage> AthleteImage { get; set; }
    

 

}
