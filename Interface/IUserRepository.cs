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
   // public Task<string> AddRole(string role);
}
