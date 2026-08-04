using sport_app_backend.Models.Chat;

namespace sport_app_backend.Dtos.Chat;

public class SendMessageDto
{
    public long ConversationId { get; set; }

    public string? Text { get; set; }


}