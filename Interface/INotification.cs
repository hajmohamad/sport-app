using sport_app_backend.Controller;
using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface INotification
{
    Task<ApiResponse> AddNewSubscribe(string phoneNumber, NotificationSubscriptionDto notification);
    Task SendPushNotification(int userId, string title, string body);

}