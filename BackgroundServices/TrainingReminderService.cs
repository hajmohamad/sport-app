using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Models.Account;

namespace sport_app_backend.BackgroundServices;

public class TrainingReminderService(IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(10);

    private readonly string[] _messages =
    {
        "موفقیت با استمرار ساخته میشه ، وقت تمرین شده!",
        "پیشرفت، تکرار قدم های کوچیک روزانه است. بیا تمرین کن!",
        "با تمرین امروز یه قدم به هدفی که داری نزدیکتر شو",
        "یادتِ برای چی شروع کردی؟ وقتشه یه قدم دیگه بهش نزدیکتر بشی"
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var push = scope.ServiceProvider.GetRequiredService<IWebPushNotificationService>();
        
            var now = DateTime.UtcNow;
        
            var athletes = await db.NotificationSubscriptions
                .Where(s => s.Role == TypeOfUser.ATHLETE)
                .Join(
                    db.Athletes.Include(a => a.ActiveWorkoutProgram),
                    sub => sub.UserId,
                    athlete => athlete.UserId,
                    (sub, athlete) => new { sub, athlete }
                )
                .Where(x =>
                    x.athlete.ActiveWorkoutProgram != null &&
                    x.athlete.ActiveWorkoutProgram.LastExerciseDate != null
                )
                .ToListAsync(stoppingToken);
        
            foreach (var athlete in athletes)
            {
                var last = athlete.athlete.ActiveWorkoutProgram!.LastExerciseDate!.Value;
        
                var daysPassed = (now.Date - last.Date).Days;
                if (daysPassed < 1) continue;
        
                var sendTime = last.Date
                    .AddDays(daysPassed)
                    .Add(last.TimeOfDay)
                    .AddMinutes(-30);
        
                if (now < sendTime || now > sendTime.Add(Tolerance))
                    continue;
        
                if (athlete.sub.LastTrainingReminderSentAtUtc.Date == now.Date)
                    continue;
        
                var msgIndex = daysPassed <= 3 ? daysPassed - 1 : 3;
        
                await push.SendAsync(
                    athlete.sub,
                    "یادآوری تمرین",
                    _messages[msgIndex]
                );
        
                athlete.sub.LastTrainingReminderSentAtUtc = now;
            }
        
            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}