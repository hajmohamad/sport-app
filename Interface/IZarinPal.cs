using sport_app_backend.Dtos.ZarinPal;

namespace sport_app_backend.Interface;

public interface IZarinPal
{
    Task<ZarinPalPaymentResponseDto> RequestPaymentAsync(ZarinPalPaymentRequestDto request);

}