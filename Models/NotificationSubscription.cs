using sport_app_backend.Models.Account;

namespace sport_app_backend.Models;

public class NotificationSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public TypeOfUser Role { get; set; }

    public string Endpoint { get; set; } = null!;
    public string? P256DH { get; set; } = null!;
    public string Auth { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.Now;
    public DateTime LastTrainingReminderSentAtUtc { get; set; } = DateTime.Now.AddDays(-1);
    
}