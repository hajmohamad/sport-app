namespace sport_app_backend.Dtos.ZarinPal.Verify;

public class ZarinPalVerifyRequestDto
{
    public string MerchantId { get; set; } 
    public double Amount { get; set; }
    public string Authority { get; set; }
}