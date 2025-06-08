using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface
{
    public interface IAdminRepository
    {
        Task<ApiResponse> AddExercises(List<AddExercisesRequestDto> exercises);
        Task<ApiResponse> ConfirmTransactionId(string TransactionId);
        Task<ApiResponse> BackfillWorkoutProgramStats();

    }
}