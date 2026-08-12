namespace sport_app_backend.Dtos.Chat;

public class CoachChatListDto
{
    public ChatListItemDto? Support { get; set; }
    public ChatListItemDto? SupportChannel { get; set; }

    public List<ChatListItemDto> Active { get; set; } = [];
    public List<ChatListItemDto> NeedsFollowUp { get; set; } = [];
    public List<ChatListItemDto> NearingCompletion { get; set; } = [];
    public List<ChatListItemDto> Inactive { get; set; } = [];

    public List<ChatListItemDto> All { get; set; } = [];
}
