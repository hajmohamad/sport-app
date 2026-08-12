namespace sport_app_backend.Models.Chat;

public class Conversation
{
    public long Id { get; set; }

    public ConversationType Type { get; set; } = ConversationType.CoachAthlete;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastMessageAt { get; set; }

    public string? LastMessageText { get; set; }

    public long? LastMessageId { get; set; }

    public bool IsClosed { get; set; } = false;

    public DateTime? ClosedAt { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public enum ConversationType
{
    CoachAthlete = 1,
    UserSupport = 2
}
