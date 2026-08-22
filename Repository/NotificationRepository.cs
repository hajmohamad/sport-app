using Microsoft.EntityFrameworkCore;
using sport_app_backend.Controller;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;

namespace sport_app_backend.Repository;

public class NotificationRepository(ApplicationDbContext db,IWebPushNotificationService webPushNotificationService): INotification
{
    public async Task<ApiResponse> AddNewSubscribe(string phoneNumber, NotificationSubscriptionDto notificationDto)
    {
        var user = await db.Users.Where(e => e.PhoneNumber == phoneNumber).Select(u =>new
        {
            u.Id,
            u.TypeOfUser
        }).FirstOrDefaultAsync();
        if (user == null)
            return new ApiResponse()
            {
                Action = false,
                Message = "User not found"
            };
        var existing = await db.NotificationSubscriptions
            .FirstOrDefaultAsync(n =>
                n.UserId == user.Id &&
                n.Endpoint == notificationDto.Endpoint &&
                n.P256DH == notificationDto.P256dh &&
                n.Auth == notificationDto.Auth
            );

        if (existing != null)
        {
             await webPushNotificationService.SendAsync(existing, "چارست", "نوتیفیکشن قبلا  برای شما فعال شده است");

            return new ApiResponse()
            {
                Action = false,
                Message = "Notification subscription already exists"
            };
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
        await webPushNotificationService.SendAsync(newEntity, "چارست", "نوتیفیکشن با موفقیت برای شما فعال شد");
        

        webPushNotificationService.SendAsync(newEntity, "چارست", "نوتیفیکشن با موفقیت برای شما فعال شد");
        
        return new ApiResponse()
        {
            Action = true,
            Message = "Notification Subscription added",
        };
    }
    public async Task SendPushNotification(int userId, string title, string body)
    {
        var subscriptions = await db.NotificationSubscriptions
            .Where(n => n.UserId == userId)
            .ToListAsync();

        if (!subscriptions.Any())
            return;

        var sendTasks = subscriptions
            .Select(sub => webPushNotificationService.SendAsync(sub, title, body));

        await Task.WhenAll(sendTasks);
    }

}