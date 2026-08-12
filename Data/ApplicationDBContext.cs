using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Actions.CouchExercise;
using sport_app_backend.Models.Challenge_Achievement;
using sport_app_backend.Models.Chat;
using sport_app_backend.Models.Login_Sinup;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Program.WorkoutProgramTemplate;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.Support;
using sport_app_backend.Models.TrainingPlan;
using sport_app_backend.Models.UserExternalAccount;
using WebPush;


namespace sport_app_backend.Data;

public class ApplicationDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);


    modelBuilder.Entity<User>()
        .HasOne(u => u.Athlete)
        .WithOne(a => a.User)
        .HasForeignKey<User>(u => u.AthleteId) 
        .OnDelete(DeleteBehavior.SetNull);

    modelBuilder.Entity<User>()
        .HasOne(u => u.Coach)
        .WithOne(c => c.User)
        .HasForeignKey<User>(u => u.CoachId) // این خط خطا را حل می‌کند
        .OnDelete(DeleteBehavior.SetNull);

  
    modelBuilder.Entity<User>()
        .HasIndex(u => u.RefreshToken)
        .HasDatabaseName("IX_User_RefreshToken"); 

    modelBuilder.Entity<User>()
        .HasIndex(u => u.SiteRefreshToken)
        .HasDatabaseName("IX_User_SiteRefreshToken");

    modelBuilder.Entity<User>()
        .HasIndex(u => u.PhoneNumber)
        .IsUnique(); 

    modelBuilder.Entity<Athlete>()
        .HasMany(a => a.WorkoutPrograms)
        .WithOne(w => w.Athlete)
        .HasForeignKey(w => w.AthleteId)
        .OnDelete(DeleteBehavior.Cascade);
    
    modelBuilder.Entity<Coach>()
        .HasOne(c => c.CoachCardNumber)
        .WithOne(cc => cc.Coach)
        .HasForeignKey<CoachCardNumber>(cc => cc.CoachId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Athlete>()
        .HasOne(a => a.ActiveWorkoutProgram)
        .WithMany()
        .HasForeignKey(a => a.ActiveWorkoutProgramId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<DiscountCode>()
        .HasIndex(x => x.Code)
        .IsUnique();

    modelBuilder.Entity<Payment>()
        .HasOne(x => x.DiscountCode)
        .WithMany(x => x.Payments)
        .HasForeignKey(x => x.DiscountCodeId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<DiscountCodeCoachService>()
        .HasKey(dcs => new { dcs.DiscountCodeId, dcs.CoachServiceId });

    modelBuilder.Entity<DiscountCodeCoachService>()
        .HasOne(dcs => dcs.DiscountCode)
        .WithMany(dc => dc.DiscountCodeCoachServices)
        .HasForeignKey(dcs => dcs.DiscountCodeId);

    modelBuilder.Entity<DiscountCodeCoachService>()
        .HasOne(dcs => dcs.CoachService)
        .WithMany(cs => cs.DiscountCodeCoachServices)
        .HasForeignKey(dcs => dcs.CoachServiceId);
    modelBuilder.Entity<UserExternalAccount>()
        .HasIndex(x => new { x.Provider, x.ProviderUserId })
        .IsUnique();


    modelBuilder.Entity<EitaaLoginSession>()
        .HasIndex(x => x.TokenHash)
        .IsUnique();

    modelBuilder.Entity<UserExternalAccount>()
        .HasOne(x => x.User)
        .WithMany(x => x.ExternalAccounts)
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    
    modelBuilder.Entity<WorkoutProgramTemplate>()
        .HasOne(x => x.Coach)
        .WithMany()
        .HasForeignKey(x => x.CoachId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TemplateProgramInDay>()
        .HasOne(x => x.WorkoutProgramTemplate)
        .WithMany(x => x.ProgramInDays)
        .HasForeignKey(x => x.WorkoutProgramTemplateId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TemplateSingleExercise>()
        .HasOne(x => x.TemplateProgramInDay)
        .WithMany(x => x.AllExerciseInDays)
        .HasForeignKey(x => x.TemplateProgramInDayId)
        .OnDelete(DeleteBehavior.Cascade);
    
    modelBuilder.Entity<Conversation>(entity =>
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Type).IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();

        entity.Property(x => x.LastMessageText)
            .HasMaxLength(1000);

        entity.HasMany(x => x.Participants)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(x => x.Messages)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.LastMessageAt);
    });

    modelBuilder.Entity<ConversationParticipant>(entity =>
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.JoinedAt).IsRequired();

        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique();
    });

    modelBuilder.Entity<ChatMessage>(entity =>
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Text)
            .HasMaxLength(4000);



        entity.Property(x => x.SentAt).IsRequired();

        entity.HasOne(x => x.SenderUser)
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => new { x.ConversationId, x.Id });

        
    });

   
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
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<TicketMessage> TicketMessages { get; set; }

    public DbSet<InjuryArea> InjuryAreas { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<TrainingSession> TrainingSessions  { get; set; }
    public DbSet<ExerciseFeedback> ExerciseFeedbacks { get; set; }
    public DbSet<ExerciseChangeRequest> ExerciseChangeRequests { get; set; }
    public DbSet<CoachPayout> CoachPayouts { get; set; }
    public DbSet<CoachFaq> CoachFaq { get; set; }
    public DbSet<AthleteFaq> AthleteFaq { get; set; }
    public DbSet<AthleteBodyImage> AthleteImage { get; set; }
    public DbSet<NotificationSubscription> NotificationSubscriptions { get; set; }
    public DbSet<InAppMessage> InAppMessages { get; set; }
    public DbSet<UserMessageStatus> UserMessageStatuses { get; set; }
    public DbSet<WorkoutProgramFeedback> WorkoutProgramFeedback { get; set; }
    public DbSet<DiscountCode> DiscountCodes { get; set; }
    public DbSet<CoachPineExercise>  CoachPineExercises { get; set; }
    public DbSet<LastWorkoutExercise>  LastWorkoutExercises { get; set; }
    public DbSet<PaymentAttempt>  PaymentAttempts { get; set; }
    public DbSet<CoachCardNumber>  CoachCardNumbers { get; set; }
    public DbSet<DiscountCodeCoachService> DiscountCodeCoachServices { get; set; }
    public DbSet<AthleteChangePhoto> AthleteChangePhotos { get; set; }
    
    public DbSet<UserExternalAccount> UserExternalAccounts => Set<UserExternalAccount>();

    public DbSet<EitaaLoginSession> EitaaLoginSessions => Set<EitaaLoginSession>();
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<WorkoutProgramTemplate> WorkoutProgramTemplates { get; set; }
    public DbSet<TemplateProgramInDay> TemplateProgramInDays { get; set; }
    public DbSet<TemplateSingleExercise> TemplateSingleExercises { get; set; }
    public DbSet<Conversation> Conversations { get; set; }

    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }

    public DbSet<ChatMessage> ChatMessages { get; set; }



    

    


}
