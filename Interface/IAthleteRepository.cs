using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Controller;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Models;

namespace sport_app_backend.Interface
{
    public interface IAthleteRepository
    {
        public Task<ApiResponse> SubmitAthleteQuestions(string phoneNumber, AthleteQuestionDto AthleteQuestionDto);
        public Task<ApiResponse> AthleteFirstQuestions(string phoneNumber, AthleteFirstQuestionsDto athleteFirstQuestionsDto);
        public Task<ApiResponse> AddWaterIntake(string phoneNumber, WaterInTakeDto waterInTakeDto);
        public Task<ApiResponse> UpdateWaterInDay(string phoneNumber);
        public Task<ApiResponse> UpdateGoalWeight(string phoneNumber, double goalWeight);
        public Task<ApiResponse> UpdateWeight(string phoneNumber, double weight);
        public Task<ApiResponse> UpdateHightWeight(string phoneNumber, double weight, int hight);


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
        public Task<ApiResponse> GetAllTrainingSession(string phoneNumber);
        public Task<ApiResponse> GetTrainingSession(string phoneNumber, int trainingSessionId);
        public Task<ApiResponse> DoTrainingSession(string phoneNumber, int trainingSessionId, int exerciseNumber);
        public Task<ApiResponse> FinishTrainingSession(string phoneNumber, FinishTrainingSessionDto finishTrainingSessionDto);
        public Task<ApiResponse> FeedbackTrainingSession(string phoneNumber, FeedbackTrainingSessionDto feedbackTrainingSessionDto);
        public Task<ApiResponse> ResetTrainingSession(string phoneNumber, int trainingSessionId);
        public Task<ApiResponse> VerifyPaymentAsync(ZarinPalVerifyRequestDto request);
    }
}