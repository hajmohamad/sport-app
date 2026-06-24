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
        var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();

        // استفاده از تاریخ امروز
        var today = DateTime.Today;

        var paymentAttempts = await db.PaymentAttempts
            .Where(pa => !pa.SmsIsSend && 
                         pa.DateTime < DateTime.Now.AddHours(-1) && // فقط موارد قدیمی‌تر از 1 ساعت
                         !pa.Athlete.Payments.Any(p => p.PaymentDate.Date == today && p.PaymentStatus == PaymentStatus.SUCCESS))
            .Include(pa => pa.CoachService)
            .Include(pa => pa.Coach).ThenInclude(c => c.User)
            .Include(pa => pa.Athlete)
            .ToListAsync(stoppingToken);

        foreach (var paymentAttempt in paymentAttempts)
        {
            try
            {
                var phone = paymentAttempt.Athlete?.PhoneNumber;
                if (string.IsNullOrWhiteSpace(phone)) continue;

                var coachName = $"{paymentAttempt.Coach?.User?.FirstName} {paymentAttempt.Coach?.User?.LastName}".Trim();
                var websiteUrl = $"https://chaarset.ir/coach/{paymentAttempt.Coach?.WebSiteUrl}/";

                var message = $"فقط یک قدم باقی مونده!\n" +
                              $"{paymentAttempt.CoachService?.Title} رو همین الان از {coachName} دریافت کن تا به هدفت برسی:\n" +
                              $"{websiteUrl}";

                await sms.SendSms(phone, message);

                paymentAttempt.SmsIsSend = true;
                
                logger.LogInformation("SMS sent to {Phone} for Attempt {Id}", phone, paymentAttempt.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send SMS for PaymentAttempt {Id}", paymentAttempt.Id);
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }

}