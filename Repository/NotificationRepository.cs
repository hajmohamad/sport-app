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
        var newEntity = new NotificationSubscription
        {
            UserId = user.Id,
            Role = user.TypeOfUser,
            Endpoint = notificationDto.Endpoint,
            P256DH = notificationDto.Keys.P256dh,
            Auth = notificationDto.Keys.Auth,

        };
        db.NotificationSubscriptions.Add(newEntity);
        await db.SaveChangesAsync();
        var response = webPushNotificationService.SendAsync(newEntity, "چارست", "نوتیفیکشن با موفقیت برای شما فعال شد");
        
        return new ApiResponse()
        {
            Action = true,
            Message = "Notification Subscription added",
        };
    }
}