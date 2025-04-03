using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface
{
    public interface ICoachRepository
    {
        Task<ApiResponse> SubmitCoachQuestions(string phoneNumber, CoachQuestionDto coachQuestionDto);
        Task<ApiResponse> AddCoachingPlane(string phoneNumber, AddCoachingPlaneDto addCoachingPlaneDto);
        Task<ApiResponse> UpdateCoachingPlane(string phoneNumber,int id, AddCoachingPlaneDto addCoachingPlaneDto);
        Task<ApiResponse> DeleteCoachingPlane(string phoneNumber,int id);
        Task<ApiResponse> GetAllPayment(string phoneNumber);
        Task<ApiResponse> GetPayment(string phoneNumber,int paymentId);
    }
}
