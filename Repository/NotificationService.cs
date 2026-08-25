// Services/NotificationService.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Notification;
using sport_app_backend.Interface;
using sport_app_backend.Models;

namespace sport_app_backend.Repository;

public class NotificationService(
    ApplicationDbContext db,
    INotificationQueue notificationQueue,
    IMemoryCache cache)
    : INotificationService
{

    public async Task<ApiResponse> AddNewSubscribeAsync(string phoneNumber, NotificationSubscriptionDto notificationDto)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PhoneNumber == phoneNumber);

        if (user == null)
            return new ApiResponse { Action = false, Message = "User not found" };

        var existing = await db.NotificationSubscriptions
            .AsNoTracking()
            .AnyAsync(n => n.UserId == user.Id && n.Endpoint == notificationDto.Endpoint);

        if (existing)
        {
            return new ApiResponse { Action = false, Message = "Notification subscription already exists" };
        }

        var newEntity = new NotificationSubscription
        {
            UserId = user.Id,
            Role = user.TypeOfUser,
            Endpoint = notificationDto.Endpoint,
            P256DH = notificationDto.P256dh,
            Auth = notificationDto.Auth,
        };

        db.NotificationSubscriptions.Add(newEntity);
        await db.SaveChangesAsync();

    
        var cacheKey = $"subscriptions_{user.Id}";
        cache.Remove(cacheKey);

        await SendPushNotificationAsync(user.Id, "چارست", "نوتیفیکشن با موفقیت برای شما فعال شد");

        return new ApiResponse { Action = true, Message = "Notification Subscription added" };
    }

    public async Task SendPushNotificationAsync(int userId, string title, string body)
    {
        var request = new NotificationRequest(userId, title, body);
        await notificationQueue.EnqueueAsync(request);
    }
}