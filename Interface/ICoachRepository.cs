using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models;

namespace sport_app_backend.Interface
{
    public interface ICoachRepository
    {
        Task<ApiResponse> SubmitCoachQuestions(string phoneNumber, CoachQuestionDto coachQuestionDto);
        Task<ApiResponse> AddCoachingServices(string phoneNumber, AddCoachServiceDto addCoachingServiceDto);
        Task<ApiResponse> UpdateCoachingService(string phoneNumber,int id, AddCoachServiceDto addCoachingServiceDto);
        Task<ApiResponse> DeleteCoachingService(string phoneNumber,int id);
        Task<ApiResponse> GetAllPayment(string phoneNumber);
        Task<ApiResponse> GetPayment(string phoneNumber,int paymentId);
        Task<ApiResponse> GetExercises();
        Task<ApiResponse> GetProfile(string phoneNumber);
        Task<ApiResponse> SaveWorkoutProgram(string phoneNumber,int paymentId, WorkoutProgramDto saveWorkoutProgramDto);
        Task<ApiResponse> GetWorkoutProgram(string phoneNumber, int paymentId);
    }
}
