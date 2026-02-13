using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IWebPushNotificationService
{
    public Task SendAsync(NotificationSubscription sub, string title, string body);

}