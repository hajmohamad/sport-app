using System.Text.Json.Serialization;

namespace sport_app_backend.Dtos;

public abstract class SubscriptionKeysDto
{
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
    public SubscriptionKeysDto() { }

    [JsonConstructor]
    public SubscriptionKeysDto(string p256dh, string auth)
    {
        P256dh = p256dh;
        Auth = auth;
    }
}