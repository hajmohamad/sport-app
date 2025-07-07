using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Dtos.ZarinPal.Verify;

namespace sport_app_backend.Interface;

public interface IZarinPal
{
    Task<ZarinPalPaymentResponseDto> RequestPaymentAsync(ZarinPalPaymentRequestDto request);
    Task<ZarinpalVerifyApiResponseDto> VerifyPaymentAsync(ZarinPalVerifyRequestDto request);
}