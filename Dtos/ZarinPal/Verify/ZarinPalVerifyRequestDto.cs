namespace sport_app_backend.Dtos.ZarinPal.Verify;

public class ZarinPalVerifyRequestDto
{
    public string MerchantId { get; set; } = "ff39b97f-abef-43d4-adc6-ebc3033f3f3b";
    public string Authority { get; set; }
    public int Amount { get; set; }
}