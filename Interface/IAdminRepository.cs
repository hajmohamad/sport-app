using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using sport_app_backend.Dtos;
using sport_app_backend.Models;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Support;
using sport_app_backend.Services;

namespace sport_app_backend.Interface
{
    public interface IAdminRepository
    {
        
        Task<ApiResponse> VerifiedCoach(string coachPhoneNumber);
         Task<ApiResponse> GetAllCoachPayouts();
         Task<ApiResponse> UpdateCoachPayoutStatus(int payoutId, PayoutStatus newStatus, string? transactionReference,
             IFormFile file);
         Task<ApiResponse> GetCoachService(string coachWebSiteUrl);
         // Task<SmsResponse> SendMassageToCoach( string phoneNumber, string message);
         Task<ApiResponse> GetVerifiedCoaches();
         Task<ApiResponse> ActiveShowWebsiteCoach(string coachPhoneNumber);
         Task<ApiResponse> GetAllSupportTicketsAsync(TicketStatus? status = null);
         Task<ApiResponse> GetSupportTicketDetailsAsync(int ticketId);
         Task<ApiResponse> ReplyToSupportTicketAsync(int ticketId, ReplyTicketDto dto);
         Task<ApiResponse> CloseSupportTicketAsync(int ticketId);
    }
}