namespace sport_app_backend.Controller;

public abstract class SubscriptionKeysDto
{
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
}