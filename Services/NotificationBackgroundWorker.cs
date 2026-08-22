// Workers/NotificationBackgroundWorker.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Models;

namespace sport_app_backend.Services;

public class NotificationBackgroundWorker(
    ILogger<NotificationBackgroundWorker> logger,
    INotificationQueue queue,
    IServiceProvider serviceProvider, // برای ایجاد scope جدید
    IMemoryCache cache)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Background Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // منتظر یک درخواست جدید در صف می‌مانیم
                var request = await queue.DequeueAsync(stoppingToken);

                // برای هر درخواست، یک Scope جدید ایجاد می‌کنیم تا به سرویس‌های Scoped
                // مانند DbContext و IWebPushNotificationService دسترسی پیدا کنیم.
                // این کار بسیار مهم است چون BackgroundService یک Singleton است.
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var webPushService = scope.ServiceProvider.GetRequiredService<IWebPushNotificationService>();

                    // ابتدا در کش به دنبال اشتراک‌ها می‌گردیم
                    var cacheKey = $"subscriptions_{request.UserId}";
                    if (!cache.TryGetValue(cacheKey, out List<NotificationSubscription> subscriptions))
                    {
                        // اگر در کش نبود، از دیتابیس می‌خوانیم
                        subscriptions = await dbContext.NotificationSubscriptions
                            .AsNoTracking()
                            .Where(n => n.UserId == request.UserId)
                            .ToListAsync(stoppingToken);

                        // و نتیجه را برای مدتی در کش ذخیره می‌کنیم (مثلاً ۱ ساعت)
                        cache.Set(cacheKey, subscriptions, TimeSpan.FromHours(1));
                    }
                    
                    if (subscriptions.Any())
                    {
                        logger.LogInformation("Sending notification to user {UserId} with {SubscriptionCount} devices.", request.UserId, subscriptions.Count);
                        
                        var sendTasks = subscriptions
                            .Select(sub => webPushService.SendAsync(sub, request.Title, request.Body));
                        
                        await Task.WhenAll(sendTasks);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // این خطا زمانی رخ می‌دهد که برنامه در حال خاموش شدن است. طبیعی است.
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing a notification request.");
                // منتظر می‌مانیم تا از حلقه دیوانه‌وار در صورت خطاهای مکرر جلوگیری کنیم
                await Task.Delay(5000, stoppingToken);
            }
        }
        logger.LogInformation("Notification Background Worker is stopping.");
    }
}