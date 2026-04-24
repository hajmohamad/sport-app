using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Coach
{
    public interface ICoachRepository
    {
        Task<ApiResponse> SubmitCoachQuestions(int coachId, CoachQuestionDto coachQuestionDto);
        Task<ApiResponse> AddCoachingServices(int coachId, AddCoachServiceDto addCoachingServiceDto);
        Task<ApiResponse> UpdateCoachingService(int coachId,int id, AddCoachServiceDto addCoachingServiceDto);
        Task<ApiResponse> DeleteCoachingService(int coachId,int id);
        Task<ApiResponse> GetAllPayment(int coachId);
        Task<ApiResponse> GetPayment(int coachId,int paymentId);
        Task<ApiResponse> GetProfile(int coachId);
        Task<ApiResponse> SaveWorkoutProgram(int coachId,int paymentId, WorkoutProgramDto saveWorkoutProgramDto);
        Task<ApiResponse> GetWorkoutProgram(int coachId, int paymentId);
        Task<ApiResponse> GetCoachDashboard(int coachId);
        Task<ApiResponse> GetMonthlyIncomeChart(int coachId, int year, int month);
        Task<ApiResponse> UpdateSocialMediaLink(int coachId, SocialMediaLinkDto socialMediaLinkDto);
        Task<ApiResponse> GetSocialMediaLink(int coachId);
        Task<ApiResponse> GetAthletesWithStatus(int coachId);
        Task<ApiResponse> GetTransactions(int coachId);
        Task<ApiResponse> CreatePayoutRequest(int coachId);
        Task<ApiResponse> AthleteReportForCoach(int athleteId);
        Task<ApiResponse> AthleteMonthlyActivityForCoach(int athleteId, int year, int month);
        Task<ApiResponse> GetFaq();
        Task<ApiResponse> Test();
        Task<ApiResponse> GetWPkey(int workoutProgramId);
        Task<ApiResponse> GetWorkoutProgramFeedBack(int coachId);
        Task<ApiResponse> ChoseWorkoutProgramFeedBack(int coachId,List<ChoseWorkoutProgramFeedBackDto> choseWorkoutProgramFeedBackDtOs);

    }
}
