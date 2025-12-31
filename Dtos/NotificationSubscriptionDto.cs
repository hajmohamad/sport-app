namespace sport_app_backend.Dtos;

public class NotificationSubscriptionDto
{
    public required string Endpoint { get; set; }
    public required SubscriptionKeysDto Keys { get; set; } // فیلد کلیدها به عنوان یک شیء مجزا
}