using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Chat;

public class ChatMessage
{
    public long Id { get; set; }

    public long ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public ChatMessageType Type { get; set; } = ChatMessageType.Text;

    public string? Text { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    public DateTime? EditedAt { get; set; }
    
    public Conversation Conversation { get; set; } = null!;

    public User SenderUser { get; set; } = null!;
    public string? FileUrl { get; set; }
}

public enum ChatMessageType
{
    Text = 1,
    Image = 2,
    File = 3,
    System = 4
}
