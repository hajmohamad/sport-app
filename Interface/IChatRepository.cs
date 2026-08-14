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
        int senderUserId,
        long? conversationId,
        IFormFile file);

    Task<ApiResponse> CreateCoachAthleteConversation(
        int coachUserId,
        int athleteUserId);
    Task<ApiResponse> GetCoachChatList(int coachId, int coachUserId, string? status = null);

    Task<ApiResponse> GetAthleteChatList(int athleteUserId);

    Task<ApiResponse> AddSystemMessage(int coachUserId, int athleteUserId, string text);
    Task<ApiResponse> AddNewUserToChannel(int userId,bool isCoach);


    Task<ApiResponse> BackfillCoachAthleteConversationsFromSuccessfulPayments();
}