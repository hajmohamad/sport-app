using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Controller;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models;

namespace sport_app_backend.Interface
{
    public interface IAthleteRepository
    {
        public Task<ApiResponse> SubmitAthleteQuestions(string phoneNumber, AthleteQuestionDto AthleteQuestionDto);
        public Task<ApiResponse> AthleteFirstQuestions(string phoneNumber, AthleteFirstQuestionsDto athleteFirstQuestionsDto);
        public Task<ApiResponse> AddWaterIntake(string phoneNumber, WaterInTakeDto waterInTakeDto);
        public Task<ApiResponse> UpdateWaterInDay(string phoneNumber);
        public Task<ApiResponse> UpdateWeight(string phoneNumber, double weight);
        public Task<ApiResponse> WeightReport(string phoneNumber);

        public Task<ApiResponse> AddActivity(string phoneNumber, AddActivityDto addSportDto);
        public Task<ApiResponse> DeleteActivity(string phoneNumber,int activityId);

        public Task<ApiResponse> ActivityReport(string phoneNumber);
        public Task<ApiResponse> BuyCoachingService(string phoneNumber,int coachingServiceId);
        public Task<ApiResponse> SearchCoaches(CoachNameSearchDto coachNameSearchDto);
        public Task<ApiResponse> GetLastQuestion(string phoneNumber);
        public Task<ApiResponse> CompleteNewChallenge(string phoneNumber, string challenge);
        public Task<ApiResponse> CompletedChallenge(string phoneNumber);
        public Task<ApiResponse> GetAchievements(string phoneNumber);
        public Task<ApiResponse> GetAllPayments(string phoneNumber);
        public Task<ApiResponse> GetPayment(string phoneNumber,int paymentId);
        public Task<ApiResponse> GetAllPrograms(string phoneNumber);

        public Task<ApiResponse> GetProgram(string phoneNumber, int programId);
        public Task<ApiResponse> ActiveProgram(string phoneNumber, int programId);
        public Task<ApiResponse> ExerciseFeedBack(string phoneNumber, ExerciseFeedbackDto exerciseFeedbackDto);
        public Task<ApiResponse> ChangeExercise(string phoneNumber, ExerciseChangeDto changeExerciseDto);
    }
}