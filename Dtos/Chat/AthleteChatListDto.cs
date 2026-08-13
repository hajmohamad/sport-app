namespace sport_app_backend.Dtos.Chat;

public class AthleteChatListDto
{
    public ChatListItemDto? Support { get; set; }
    public List<ChatListItemDto?> Channels { get; set; } = [];
    public List<ChatListItemDto> Coaches { get; set; } = [];

}