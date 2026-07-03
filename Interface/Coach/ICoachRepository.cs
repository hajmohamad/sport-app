using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Coach;
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
        Task<ApiResponse> CreateDiscountCode(int coachId, DiscountCodeCreateDto discountCodeCreateDto);
        Task<ApiResponse> UpdateDiscountCode(int coachId, int discountCodeId, DiscountCodeUpdateDto discountCodeUpdateDto);
        Task<ApiResponse> GetDiscountCodes(int coachId);
        Task<ApiResponse> GetDiscountCodeById(int coachId, int discountCodeId);
        Task<ApiResponse> GetAllServicesWithCalculatedDiscount(int coachId, int percent);

        Task<ApiResponse> ChangeStatusForDiscountCode(int coachId, int discountCodeId, string status);
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
        Task<ApiResponse> AddCardNumber(int coachId, AddCardNumberDto addCardNumberDto);
        Task<ApiResponse>  GetCardNumber(int coachId);
        Task<ApiResponse> GetAllChangePhotos(int coachId);
        Task<ApiResponse> GetChangePhotoById(int coachId, int id);
        Task<ApiResponse> AddChangePhoto(int coachId, IFormFile file, AddAthleteChangePhotoDto dto);
        Task<ApiResponse> EditChangePhoto(int coachId, IFormFile? file, EditAthleteChangePhotoDto dto);
        Task<ApiResponse> DeleteChangePhoto(int coachId, int id);
        Task<ApiResponse> GetWebSiteUrlStatusAsync(int coachId);
        Task<ApiResponse> UpdateWebSiteUrlAsync(int coachId, string newUrl);
        Task<ApiResponse> CheckWebSiteUrlAvailabilityAsync(int coachId, string url);

        Task<ApiResponse> GetCoachChecklist(string phoneNumber);
    }
}
