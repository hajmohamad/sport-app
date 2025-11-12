using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using sport_app_backend.Dtos;
using sport_app_backend.Models;
using sport_app_backend.Models.Payments;
using sport_app_backend.Services;

namespace sport_app_backend.Interface
{
    public interface IAdminRepository
    {
        Task<ApiResponse> AddExercises(AddExercisesRequestDto exercises);
        Task<ApiResponse> ConfirmTransactionId(string TransactionId);
        Task<ApiResponse> BackfillWorkoutProgramStats();

        Task<ApiResponse> VerifiedCoach(string coachPhoneNumber);
         Task<ApiResponse> GetAllCoachPayouts();
         Task<ApiResponse> UpdateCoachPayoutStatus(int payoutId, PayoutStatus newStatus, string? transactionReference,
             IFormFile file);

         Task<ApiResponse> GetCoachService(string phoneNumber);
         Task<SmsResponse> SendMassageToCoach( string phoneNumber, string message);
         Task<ApiResponse> GetSupportApp();
         Task<ApiResponse> AddSlug(string engName, string slug);
         Task<ApiResponse> AnswerSupportApp(int id);
    }
}