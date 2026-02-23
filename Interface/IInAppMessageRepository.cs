using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IInAppMessageRepository
{
    Task<ApiResponse> CreateMessage(CreateInAppMessageDto dto);
    Task<ApiResponse> GetMessages(string phoneNumber);
    Task<ApiResponse> GetMessageDetail(string phoneNumber, int messageId);
    Task<ApiResponse> MarkAsRead(string phoneNumber, int messageId);
    Task<ApiResponse> MarkAllAsRead(string phoneNumber);
    Task<int> GetUnreadCount(string phoneNumber);
}