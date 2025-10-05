using sport_app_backend.Dtos;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Interface;

public interface IUserRepository
{
    public Task<ApiResponse> Login(string UserPhoneNumber);
    public Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto);
    public Task<ApiResponse> AddRoleGender(string phoneNumber, RoleGenderDto roleGenderDto);
    public  Task<ApiResponse> GenerateAccessToken(string refreshToken);
   
    public Task<ApiResponse> EditUserProfile(string phoneNumber, EditUserProfileDto editUserProfileDto);
    public Task<ApiResponse> GetUserProfileForEdit(string phoneNumber);
    public Task<ApiResponse> Logout(string phoneNumber);
    public Task<ApiResponse> AppSupport(string phoneNumber, ReportAppDto reportAppDto);
    public Task<ApiResponse> SaveImageAsync(string phoneNumber,IFormFile file);
    public Task<ApiResponse> GetAllExercise();
    public ApiResponse UpdateApp();


    public Task<ApiResponse> GetExercise(int exerciseId);

    public Task<ApiResponse> RemoveProfilePhoto(string phoneNumber);
    public Task<ApiResponse> CreateWorkoutPdfAsync(string wpId);

    public Task<ApiResponse> CheckQuestionSubmitted(string phoneNumber);
    Task<(IEnumerable<AllExerciseResponseDto> Exercises, int TotalCount)> GetExercisesAsync(
        string? level,
        string? type,
        string? equipment,
        string? muscle,
        string? place,
        int page,
        int pageSize
    );
}
