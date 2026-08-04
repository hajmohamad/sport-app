using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Chat;

public class ConversationParticipant
{
    public long Id { get; set; }

    public long ConversationId { get; set; }

    public int UserId { get; set; }

    public ConversationParticipantRole Role { get; set; }

    public long? LastReadMessageId { get; set; }

    public DateTime? LastReadAt { get; set; }

    public bool IsMuted { get; set; } = false;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;

    public User User { get; set; } = null!;
}


public enum ConversationParticipantRole
{
    Coach = 2,
    Athlete = 3,
    Support = 4,
    Admin = 5,
    User = 6

    
}
