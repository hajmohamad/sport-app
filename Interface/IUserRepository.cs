using sport_app_backend.Dtos;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Interface;

public interface IUserRepository
{
    public Task<ApiResponse> Login(string UserPhoneNumber);
    public Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto);
    public Task<ApiResponse> AddRoleGender(int userId, RoleGenderDto roleGenderDto);
    public  Task<ApiResponse> GenerateAccessToken(string refreshToken);
   
    public Task<ApiResponse> EditUserProfile(int userId, EditUserProfileDto editUserProfileDto);
    public Task<ApiResponse> GetUserProfileForEdit(int userId);
    public Task<ApiResponse> Logout(int userId);
    public Task<ApiResponse> AppSupport(int userId, ReportAppDto reportAppDto);
    public Task<ApiResponse> SaveImageAsync(int userId,IFormFile file);
    public Task<ApiResponse> GetAllExercise();
    public Task<ApiResponse> GetExercise(int exerciseId);

    public Task<ApiResponse> RemoveProfilePhoto(int userId);
    public Task<ApiResponse> CreateWorkoutPdfAsync(string wpId);

    public Task<ApiResponse> CheckQuestionSubmitted(int userId);
    Task<(IEnumerable<AllExerciseResponseDto> Exercises, int TotalCount)> GetExercisesAsync(string? level,
        string? type,
        string? mechanic,
        string?[] equipment,
        string? muscle,
        string? place,
        int page,
        int pageSize, string? searchTerm);
}
