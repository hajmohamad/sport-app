using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Program;

namespace sport_app_backend.BackgroundServices;

public class QuestionReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<QuestionReminderService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddHours(21);
            
            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }
            
            var delay = nextRun - now;
            logger.LogError(
                "QuestionReminderService: Next run at {NextRun}. Waiting {Delay}",
                nextRun, delay);
            
            await Task.Delay(delay, stoppingToken);

            try
            {
                await  SendReminders(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in QuestionReminderService");
            }
        }
    }

    private async Task SendReminders(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // var push = scope.ServiceProvider.GetRequiredService<IWebPushNotificationService>();
        var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var now = DateTime.Now;
        var before5Hour = now.AddHours(-5);
        var after3Days = now.AddDays(-3);

        var activePrograms = await db.WorkoutPrograms.Where(wp =>
                wp.Status == WorkoutProgramStatus.UNCOMPLETEDQUESTION && wp.Payment.PaymentDate < before5Hour&& wp.Payment.PaymentDate > after3Days)
           .Include(workoutProgram => workoutProgram.Athlete)
            .ThenInclude(athlete => athlete.User)
            .ToListAsync(stoppingToken);

        foreach (var program in activePrograms)
        {
            var athleteName = program.Athlete.User?.FirstName ?? "ورزشکار";
            var phoneNumber = program.Athlete.PhoneNumber;
            var wpKey = tokenService.HashEncode(program.Id);
            var message = $"{athleteName}"+
                          "عزیز، فرم اطلاعات اولیه شما هنوز تکمیل نشده. لطفاً از طریق لینک زیر اقدام کنید تا برنامه تمرینی شما آماده شود:"+
                          "\n"+$"chaarset.ir/program/{wpKey}/";

            try
            {
                await sms.SendSms(phoneNumber, message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send SMS to athlete {AthleteId}",
                    program.AthleteId);
            }

            program.RenewalReminderSent = true;

            logger.LogError(
                "QuestionReminderService: New reminder sent {programId} ," +
                "by name {athleteName}, phone {phoneNumber}", program.AthleteId,
                program.Athlete.User?.FirstName + " " + program.Athlete.PhoneNumber, program.Athlete.PhoneNumber);
        }
        await db.SaveChangesAsync(stoppingToken);

    }
}