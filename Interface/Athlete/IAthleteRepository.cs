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
        public Task<ApiResponse> GetAllTrainingSession(string phoneNumber);
        public Task<ApiResponse> GetTrainingSession(string phoneNumber, int trainingSessionId);
        public Task<ApiResponse> DoTrainingSession(string phoneNumber, int trainingSessionId, int exerciseNumber);
        public Task<ApiResponse> FinishTrainingSession(string phoneNumber, FinishTrainingSessionDto finishTrainingSessionDto);
        public Task<ApiResponse> FeedbackTrainingSession(string phoneNumber, FeedbackTrainingSessionDto feedbackTrainingSessionDto);
        public Task<ApiResponse> ResetTrainingSession(string phoneNumber, int trainingSessionId);
        public Task<ApiResponse> CalculateCalories(string phoneNumber, int trainingSessionId);
        public Task<ApiResponse> AthleteFirstQuestions(int athleteId, AthleteFirstQuestionsDto athleteFirstQuestionsDto);
        public Task<ApiResponse> GetAllPayments(int athleteId);
        public Task<ApiResponse> GetPayment(int athleteId,int paymentId);
        public Task<ApiResponse> ActiveProgram(int athleteId, int paymentId);
        public Task<ApiResponse> ExerciseFeedBack(int athleteId, ExerciseFeedbackDto exerciseFeedbackDto);
        public Task<ApiResponse> ChangeExercise(int athleteId, ExerciseChangeDto changeExerciseDto);
        public Task<ApiResponse> GetAllTrainingSession(int athleteId);
        public Task<ApiResponse> GetTrainingSession(int athleteId, int trainingSessionId);
        public Task<ApiResponse> DoTrainingSession(int athleteId, int trainingSessionId, int exerciseNumber);
        public Task<ApiResponse> FinishTrainingSession(int athleteId, FinishTrainingSessionDto finishTrainingSessionDto);
        public Task<ApiResponse> FeedbackTrainingSession(int athleteId, FeedbackTrainingSessionDto feedbackTrainingSessionDto);
        public Task<ApiResponse> ResetTrainingSession(int athleteId, int trainingSessionId);
        public Task<ApiResponse> CalculateCalories(int athleteId, int trainingSessionId);
        public Task<ApiResponse> GetFaq();
        public Task<ApiResponse> WorkoutProgramFeedback(string phoneNumber, FeedbackWorkoutProgramDto feedbackWorkoutProgramDto);
        public Task<ApiResponse> WorkoutProgramFeedback(int athleteId, FeedbackWorkoutProgramDto feedbackWorkoutProgramDto);
    }
}
