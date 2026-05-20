using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;

namespace sport_app_backend.BackgroundServices;

public class PaymentAttemptSmsService(
    IServiceScopeFactory scopeFactory,
    ILogger<ProgramRenewalReminderService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddHours(22);

            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            logger.LogError(
                "PaymentAttemptSmsService: Next run at {NextRun}. Waiting {Delay}",
                nextRun, delay);
            
            await Task.Delay(delay, stoppingToken);

            try
            {
                await SendSmsPaymentAttempt(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in PaymentAttemptSmsService");
            }
        }
    }

    private async Task SendSmsPaymentAttempt(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<IWebPushNotificationService>();
        var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();

        var now = DateTime.Now;
        var targetDate = now.Date.AddHours(20);
        var paymentAttempts = await db.PaymentAttempts
            .Where(pa => pa.DateTime < targetDate && !pa.SmsIsSend &&
                         !pa.Athlete.Payments.Any(p =>
                             p.PaymentDate.Date == now.Date &&
                             p.PaymentStatus != PaymentStatus.SUCCESS))
            .Include(pa => pa.CoachService)
            .Include(pa => pa.Coach).ThenInclude(c => c.User)
            .Include(pa => pa.Athlete)
            .ToListAsync(stoppingToken);

        foreach (var paymentAttempt in paymentAttempts)
        {
            var message =
                $"فقط یک قدم باقی مونده!\n" +
                $"{paymentAttempt.CoachService.Title} رو همین الان از {paymentAttempt.Coach.User.FirstName+" "+paymentAttempt.Coach.User.LastName} دریافت کن تا به هدفت برسی:\n" +
                $"{paymentAttempt.Coach.WebSiteUrl}";

            await sms.SendSms(paymentAttempt.Athlete.PhoneNumber, message);

            paymentAttempt.SmsIsSend = true;
        


            logger.LogInformation(
                "Payment attempt SMS sent to {Phone} for PaymentAttempt {PaymentAttemptId}",
                paymentAttempt.Athlete.PhoneNumber,
                paymentAttempt.Id);

        }

        await db.SaveChangesAsync(stoppingToken);
    }
}