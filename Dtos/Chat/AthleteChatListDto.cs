namespace sport_app_backend.Dtos.Chat;

public class AthleteChatListDto
{
    public List<ChatListItemDto> Coaches { get; set; } = [];

    public ChatListItemDto? Support { get; set; }
    public  ChatListItemDto? SupportChannel { get; set; }
}