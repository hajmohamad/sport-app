using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Athlete;

public interface IBuyProgramFromApplications
{
    public Task<ApiResponse> UploadImageForAthleteQuestion(string phoneNumber,int id,string sideName, IFormFile file);
    public Task<ApiResponse> RemoveImageForAthleteQuestion(string phoneNumber,int id, string imageName);
    public Task<ApiResponse> GetImageForAthleteQuestion(string phoneNumber,int id);
    public Task<ApiResponse> BuyCoachingService(string phoneNumber,int coachingServiceId);
    public Task<ApiResponse> SearchCoaches(CoachNameSearchDto coachNameSearchDto);
    public Task<ApiResponse> GetLastQuestion(string phoneNumber);
    public Task<ApiResponse> SubmitAthleteQuestions(string phoneNumber, AthleteQuestionDto AthleteQuestionDto);
    public Task<ApiResponse> VerifyPaymentAsync(ZarinPalVerifyRequestDto request,string status);

}