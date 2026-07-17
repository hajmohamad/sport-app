using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Coach;

public interface IDiscountCodeRepository
{
    Task<ApiResponse> CreateDiscountCode(int coachId, DiscountCodeCreateDto discountCodeCreateDto);
    Task<ApiResponse> UpdateDiscountCode(int coachId, int discountCodeId, DiscountCodeUpdateDto discountCodeUpdateDto);
    Task<ApiResponse> GetDiscountCodes(int coachId);
    Task<ApiResponse> GetDiscountCodeById(int coachId, int discountCodeId);
    Task<ApiResponse> ChangeStatusForDiscountCode(int coachId, int discountCodeId, string status);

}