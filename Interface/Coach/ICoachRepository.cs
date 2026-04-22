using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Coach
{
    public interface ICoachRepository
    {
        Task<ApiResponse> SubmitCoachQuestions(string phoneNumber, CoachQuestionDto coachQuestionDto);
        Task<ApiResponse> AddCoachingServices(string phoneNumber, AddCoachServiceDto addCoachingServiceDto);
        Task<ApiResponse> UpdateCoachingService(string phoneNumber,int id, AddCoachServiceDto addCoachingServiceDto);
        Task<ApiResponse> DeleteCoachingService(string phoneNumber,int id);
        Task<ApiResponse> GetAllPayment(string phoneNumber);
        Task<ApiResponse> GetPayment(string phoneNumber,int paymentId);
        Task<ApiResponse> GetProfile(string phoneNumber);
        Task<ApiResponse> SaveWorkoutProgram(string phoneNumber,int paymentId, WorkoutProgramDto saveWorkoutProgramDto);
        Task<ApiResponse> GetWorkoutProgram(string phoneNumber, int paymentId);
        Task<ApiResponse> GetCoachDashboard(string phoneNumber);
        Task<ApiResponse> GetMonthlyIncomeChart(string phoneNumber, int year, int month);
        Task<ApiResponse> UpdateSocialMediaLink(string phoneNumber, SocialMediaLinkDto socialMediaLinkDto);
        Task<ApiResponse> GetSocialMediaLink(string phoneNumber);
        Task<ApiResponse> GetAthletesWithStatus(string coachPhoneNumber);
        Task<ApiResponse> GetTransactions(string coachPhoneNumber);
        Task<ApiResponse> CreatePayoutRequest(string coachPhoneNumber);
        Task<ApiResponse> AthleteReportForCoach(int athleteId);
        Task<ApiResponse> AthleteMonthlyActivityForCoach(int athleteId, int year, int month);
        Task<ApiResponse> GetFaq();
        Task<ApiResponse> Test();
        Task<ApiResponse> GetWPkey(int workoutProgramId);
        Task<ApiResponse> GetWorkoutProgramFeedBack(string phoneNumber);
        Task<ApiResponse> ChoseWorkoutProgramFeedBack(string phoneNumber,List<ChoseWorkoutProgramFeedBackDto> choseWorkoutProgramFeedBackDtOs);

    }
}
