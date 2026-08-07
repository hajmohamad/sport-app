namespace sport_app_backend.Dtos.Chat;

public class ChatListItemDto
{
    public long ConversationId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? ProfileImageUrl { get; set; }

    public string? Service { get; set; }

    public DateTime? LastExerciseDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public long? LastMessageId { get; set; }

    public string? LastMessageText { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public int UnreadCount { get; set; }

    public bool IsSupport { get; set; }
    public bool OtherUserRead { get; set; }
}