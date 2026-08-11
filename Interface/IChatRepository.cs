using sport_app_backend.Dtos.Chat;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IChatRepository
{

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
        long? conversationId,
        IFormFile file);

    Task<ApiResponse> CreateCoachAthleteConversation(
        int coachUserId,
        int athleteUserId);
    Task<ApiResponse> GetCoachChatList(int coachId, int coachUserId, string? status = null);

    Task<ApiResponse> GetAthleteChatList(int athleteId);

    Task<ApiResponse> AddSystemMessage(
        long conversationId,
        string text);

    Task<ApiResponse> BackfillCoachAthleteConversationsFromSuccessfulPayments();
}