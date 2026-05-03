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
        Task<ApiResponse> GetWPkey(int workoutProgramId);
        Task<ApiResponse> GetWorkoutProgramFeedBack(string phoneNumber);
        Task<ApiResponse> CreateDiscountCode(string phoneNumber, DiscountCodeCreateDto discountCodeCreateDto);
        Task<ApiResponse> UpdateDiscountCode(string phoneNumber, int discountCodeId, DiscountCodeUpdateDto discountCodeUpdateDto);
        Task<ApiResponse> GetDiscountCodes(string phoneNumber);
        Task<ApiResponse> GetDiscountCodeById(string phoneNumber, int discountCodeId);
        Task<ApiResponse> DisableDiscountCode(string phoneNumber, int discountCodeId);
        Task<ApiResponse> ChoseWorkoutProgramFeedBack(string phoneNumber,List<ChoseWorkoutProgramFeedBackDto> choseWorkoutProgramFeedBackDtOs);

        Task<(IEnumerable<AllExerciseResponseDto> Exercises, int TotalCount)> GetExercisesWithFilterForCoach(string? level,
            string? type,
            string? mechanic,
            string?[] equipment,
            string? muscle,
            string? place,
            int page,
            int pageSize, string? searchTerm,int? athleteId, int couchId);

        Task<ApiResponse> AddPineExercise(int exerciseId, int coachId);
        Task<ApiResponse> RemovePineExercise(int exerciseId, int coachId);

        Task<ApiResponse> GetCoachPayments(int coachId, PaymentFilterDto filter);
    }
}
