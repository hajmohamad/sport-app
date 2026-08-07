using sport_app_backend.Dtos.Chat;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Chat;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Mappers;

public static class ChatMapper
{
    public static ChatMessageDto ChatMessageDto(
        this ChatMessage message,
        int currentUserId,
        long? lastReadMessageId){

        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            UserId = message.SenderUserId,
            Type = message.Type.ToString(),
            Text = message.Text,
            SentAt = message.SentAt,
            IsMine = message.SenderUserId == currentUserId,
            FileUrl = message.FileUrl,
            IsRead = message.SenderUserId == currentUserId &&
                     lastReadMessageId.HasValue &&
                     lastReadMessageId.Value >= message.Id
        };
    }

    public static ChatListItemDto ToGeneralListItem(
        this Conversation conversation,
        User otherUser,
        int unreadCount)
    {
        return new ChatListItemDto
        {
            ConversationId = conversation.Id,
            UserId = otherUser.Id,
            FullName = GetFullName(otherUser),
            PhoneNumber = otherUser.PhoneNumber,
            ProfileImageUrl = otherUser.ImageProfile,
            Service = GetConversationService(conversation.Type),
            Status = conversation.IsClosed ? "Closed" : "Active",
            LastMessageId = conversation.LastMessageId,
            LastMessageText = conversation.LastMessageText,
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = unreadCount,
            IsSupport = conversation.Type == ConversationType.UserSupport
        };
    }

    public static ChatListItemDto ToSupportListItem(
        this Conversation conversation,
        User supportUser,
        int unreadCount)
    {
        return new ChatListItemDto
        {
            ConversationId = conversation.Id,
            UserId = supportUser.Id,
            FullName = "پشتیبانی چارست",
            PhoneNumber = supportUser.PhoneNumber,
            ProfileImageUrl = supportUser.ImageProfile,
            Service = "پشتیبانی",
            Status = "Support",
            LastMessageId = conversation.LastMessageId,
            LastMessageText = conversation.LastMessageText,
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = unreadCount,
            IsSupport = true
        };
    }

    public static string GetStatus(this WorkoutProgram program)
    {
        if (program.Status != WorkoutProgramStatus.ACTIVE)
        {
            return "Inactive";
        }

        if (program.LastExerciseDate is null ||
            program.LastExerciseDate.Value.Date < DateTime.UtcNow.Date.AddDays(-4))
        {
            return "NeedsFollowUp";
        }

        if (program.TotalSessionCount > 0 &&
            program.TotalSessionCount - program.CompletedSessionCount < 5)
        {
            return "NearingCompletion";
        }

        return "Active";
    }

    public static ChatListItemDto ToProgramListItem(
        this Conversation conversation,
        User otherUser,
        WorkoutProgram program,
        int unreadCount,
        string status)
    {
        return new ChatListItemDto
        {
            ConversationId = conversation.Id,
            UserId = otherUser.Id,
            FullName = GetFullName(otherUser),
            PhoneNumber = otherUser.PhoneNumber,
            ProfileImageUrl = otherUser.ImageProfile,
            Service = program.Title,
            LastExerciseDate = program.LastExerciseDate,
            Status = status,
            LastMessageId = conversation.LastMessageId,
            LastMessageText = conversation.LastMessageText,
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = unreadCount,
            IsSupport = false
        };
    }

    private static string GetFullName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName)
            ? user.PhoneNumber
            : fullName;
    }

    private static string GetConversationService(
        ConversationType conversationType)
    {
        return conversationType switch
        {
            ConversationType.CoachAthlete => "برنامه تمرینی",
            ConversationType.UserSupport => "پشتیبانی",
            _ => "گفتگو"
        };
    }
}
