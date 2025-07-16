using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Dtos;
using sport_app_backend.Models;
using sport_app_backend.Models.Payments;

namespace sport_app_backend.Interface
{
    public interface IAdminRepository
    {
        Task<ApiResponse> AddExercises(List<AddExercisesRequestDto> exercises);
        Task<ApiResponse> ConfirmTransactionId(string TransactionId);
        Task<ApiResponse> BackfillWorkoutProgramStats();

        Task<ApiResponse> VerifiedCoach(string coachPhoneNumber);
         Task<ApiResponse> GetAllCoachPayouts();
         Task<ApiResponse> UpdateCoachPayoutStatus(int payoutId, PayoutStatus newStatus, string? transactionReference);

    }
}