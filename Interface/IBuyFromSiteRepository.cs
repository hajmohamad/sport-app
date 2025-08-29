using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IBuyFromSiteRepository
{
    public Task<ApiResponse> Login(string userPhoneNumber);
    public Task<ApiResponse> CheckCode(CheckCodeRequestFromBuyFromSiteDto checkCodeRequestDto);
    public Task<ApiResponse>  VerifyPaymentAsync(ZarinPalVerifyRequestDto verifyRequest, string status);
    public Task<ApiResponse> GetWorkoutProgram(string wPkey);
}