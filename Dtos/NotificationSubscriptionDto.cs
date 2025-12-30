namespace sport_app_backend.Controller;

public class NotificationSubscriptionDto
{
    public required string Endpoint { get; set; }
    public required SubscriptionKeysDto Keys { get; set; } // فیلد کلیدها به عنوان یک شیء مجزا
}