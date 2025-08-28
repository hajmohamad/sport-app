using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface ISiteBuyRepository
{
    public Task<ApiResponse> Login(string UserPhoneNumber);
    public Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto);
}