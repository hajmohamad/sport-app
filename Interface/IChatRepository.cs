using sport_app_backend.Dtos.Chat;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IChatRepository
{
    Task<ApiResponse> GetMyConversations(int userId);

    Task<ApiResponse> GetConversationMessages(
        int userId,
        long conversationId,
        long? beforeMessageId,
        int take);

    Task<ApiResponse> SendMessage(
        int senderUserId,
        SendMessageDto dto);

    Task<ApiResponse> MarkAsRead(
        int userId,
        long conversationId,
        long lastReadMessageId);

    Task<ApiResponse> UploadAttachment(
        int userId,
        long conversationId,
        IFormFile file);

    Task<ApiResponse> CreateCoachAthleteConversation(
        int coachUserId,
        int athleteUserId);

    Task<ApiResponse> CreateSupportConversation(int userId);

    Task<ApiResponse> GetCoachChatList(int coachUserId);

    Task<ApiResponse> GetAthleteChatList(int athleteUserId);

    Task<ApiResponse> AddSystemMessage(
        long conversationId,
        string text);
}