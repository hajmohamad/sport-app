using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Models;

namespace sport_app_backend.BackgroundServices;

public class NotificationBackgroundWorker(
    ILogger<NotificationBackgroundWorker> logger,
    INotificationQueue queue,
    IServiceProvider serviceProvider, 
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
                var request = await queue.DequeueAsync(stoppingToken);

                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var webPushService = scope.ServiceProvider.GetRequiredService<IWebPushNotificationService>();
        
                var cacheKey = $"subscriptions_{request.UserId}";
                if (!cache.TryGetValue(cacheKey, out List<NotificationSubscription>? subscriptions))
                {
                    subscriptions = await dbContext.NotificationSubscriptions
                        .AsNoTracking()
                        .Where(n => n.UserId == request.UserId)
                        .ToListAsync(stoppingToken);
        
                    cache.Set(cacheKey, subscriptions, TimeSpan.FromHours(1));
                }
        
                if (subscriptions == null || subscriptions.Count == 0) continue;
                logger.LogInformation("Sending notification to user {UserId} with {SubscriptionCount} devices.", request.UserId, subscriptions.Count);
                        
                var sendTasks = subscriptions
                    .Select(sub => webPushService.SendAsync(sub, request.Title, request.Body));
                        
                await Task.WhenAll(sendTasks);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing a notification request.");
                await Task.Delay(5000, stoppingToken);
            }
        }
        logger.LogInformation("Notification Background Worker is stopping.");
    }
}