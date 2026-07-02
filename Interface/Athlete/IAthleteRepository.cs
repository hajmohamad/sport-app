using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Athlete
{
    public interface IAthleteRepository
    {
        public Task<ApiResponse> AthleteFirstQuestions(string phoneNumber, AthleteFirstQuestionsDto athleteFirstQuestionsDto);
        public Task<ApiResponse> GetAllPayments(string phoneNumber);
        public Task<ApiResponse> GetPayment(string phoneNumber,int paymentId);
        public Task<ApiResponse> ActiveProgram(string phoneNumber, int paymentId);
        public Task<ApiResponse> ExerciseFeedBack(string phoneNumber, ExerciseFeedbackDto exerciseFeedbackDto);
        public Task<ApiResponse> ChangeExercise(string phoneNumber, ExerciseChangeDto changeExerciseDto);
        public Task<ApiResponse> GetAllTrainingSession(int athleteId);
        public Task<ApiResponse> GetTrainingSession(int athleteId, int trainingSessionId);
        public Task<ApiResponse> FinishTrainingSession(int athleteId, FinishTrainingSessionDto finishTrainingSessionDto);
        public Task<ApiResponse> FeedbackTrainingSession(string phoneNumber, FeedbackTrainingSessionDto feedbackTrainingSessionDto);
        public Task<ApiResponse> ResetTrainingSession(string phoneNumber, int trainingSessionId);
        public Task<ApiResponse> CalculateCalories(string phoneNumber, int trainingSessionId);
        public Task<ApiResponse> GetFaq();
        public Task<ApiResponse> WorkoutProgramFeedback(string phoneNumber, FeedbackWorkoutProgramDto feedbackWorkoutProgramDto);
        // public Task<ApiResponse> DoTrainingSession(int athleteId, int trainingSessionId, int exerciseNumber);
    }
}