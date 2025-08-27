using sport_app_backend.Dtos;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;

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
    public Task<ApiResponse> ReportApp(string phoneNumber, ReportAppDto reportAppDto);
    public Task<ApiResponse> SaveImageAsync(string phoneNumber,IFormFile file);
    public Task<ApiResponse> GetAllExercise();
    public ApiResponse UpdateApp();


    public Task<ApiResponse> GetExercise(int exerciseId);

    public Task<ApiResponse> RemoveProfilePhoto(string phoneNumber);
    public Task<ApiResponse> CreateWorkoutPdfAsync(string wpId);

}
