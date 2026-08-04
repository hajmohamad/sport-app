using sport_app_backend.Models.Chat;

namespace sport_app_backend.Dtos.Chat;

public class ChatMessageDto
{
    public long Id { get; set; }

    public long ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public string SenderFullName { get; set; } = string.Empty;

    public string? SenderProfileImageUrl { get; set; }

    public string Type { get; set; }

    public string? Text { get; set; }

    public DateTime SentAt { get; set; }

    public bool IsMine { get; set; }
    
    public string? FileUrl { get; set; }
    public string? ContentType { get; set; }
}