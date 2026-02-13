namespace sport_app_backend.Dtos;

public class NotificationSubscriptionDto
{
    public required string Endpoint { get; set; }
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
}