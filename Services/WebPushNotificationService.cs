using System.Text.Json;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using WebPush;

namespace sport_app_backend.Services;

public class WebPushNotificationService(IConfiguration config):IWebPushNotificationService
{
    
    public async Task SendAsync(NotificationSubscription sub, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(config["Vapid:PublicKey"]) ||
            string.IsNullOrWhiteSpace(config["Vapid:PrivateKey"]))
        {
            throw new Exception("VAPID keys are not set in configuration!");
        }
        var vapid = new VapidDetails(
            subject: "mailto:support@charset.com",
            publicKey: config["Vapid:PublicKey"],
            privateKey: config["Vapid:PrivateKey"]
        );

        var webPushClient = new WebPushClient();

        var payload = JsonSerializer.Serialize(new
        {
            title = title,
            body = body,
            icon = "/icons/charset.png"
        });

        var subscription = new  PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);

       await webPushClient.SendNotificationAsync(subscription, payload, vapid);
    }
}