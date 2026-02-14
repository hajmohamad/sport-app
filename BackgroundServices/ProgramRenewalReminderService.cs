using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Program;

namespace sport_app_backend.BackgroundServices;

public class ProgramRenewalReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<ProgramRenewalReminderService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddHours(20);

            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            logger.LogInformation(
                "ProgramRenewalReminder: Next run at {NextRun}. Waiting {Delay}",
                nextRun, delay);

            await Task.Delay(delay, stoppingToken);

            try
            {
                await SendRenewalReminders(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ProgramRenewalReminderService");
            }
        }
    }

    private async Task SendRenewalReminders(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<IWebPushNotificationService>();
        var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();

        var now = DateTime.Now;

        var activePrograms = await db.WorkoutPrograms
            .Include(wp => wp.Athlete)
                .ThenInclude(a => a.User)
            .Include(wp => wp.Coach)
            .Where(wp =>
                wp.StartDate != null &&
                (     wp.Status == WorkoutProgramStatus.ACTIVE || wp.Status==WorkoutProgramStatus.NOTACTIVE)
            &&
                !wp.RenewalReminderSent
            )
            .ToListAsync(stoppingToken);

        foreach (var program in activePrograms)
        {
            var athleteName = program.Athlete.User?.FirstName ?? "";
            var phoneNumber = program.Athlete.PhoneNumber;
            var coachWebsite = program.Coach?.WebSiteUrl ?? "chaarset.ir";

            var programEndDate = program.StartDate!.Value.AddDays(program.ProgramDuration * 7);
            var daysSinceEnd = (now - programEndDate).Days;

            var remainingSessions = program.TotalSessionCount - program.CompletedSessionCount;

            double completionPercentage = 0;
            if (program.TotalSessionCount > 0)
            {
                completionPercentage = (double)program.CompletedSessionCount / program.TotalSessionCount;
            }

            bool isProgramExpired = now >= programEndDate;
            bool isSeventyPercentCompleted = completionPercentage >= 0.70;

            string? message = null;

            if (isProgramExpired)
            {
                message =
                    $"{athleteName} عزیز\n" +
                    $"{daysSinceEnd} روز از آخرین برنامه تمرینی که دریافت کردی گذشته. " +
                    $"برای دریافت برنامه جدیدت از لینک زیر به مربی خودت درخواست بده\n\n" +
                    coachWebsite;
            }
            else if (isSeventyPercentCompleted)
            {
                message =
                    $"{athleteName} عزیز\n" +
                    $"کمتر از {remainingSessions} جلسه از برنامه تمرینیت باقی مونده. " +
                    $"برای دریافت برنامه جدیدت از لینک زیر به مربی خودت درخواست بده\n\n" +
                    coachWebsite;
            }

            if (message == null) continue;

            try
            {
                await sms.SendSms(phoneNumber, message);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to send SMS to athlete {AthleteId}",
                    program.AthleteId);
            }

            program.RenewalReminderSent = true;

            logger.LogInformation(
                "Renewal reminder sent to Athlete {AthleteId} for Program {ProgramId}. " +
                "Expired: {IsExpired}, Completion: {Completion:P0}, RemainingSession: {Remaining}",
                program.AthleteId, program.Id, isProgramExpired, completionPercentage, remainingSessions);
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}