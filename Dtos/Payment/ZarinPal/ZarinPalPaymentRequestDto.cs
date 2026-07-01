namespace sport_app_backend.Dtos.ZarinPal;

public class ZarinPalPaymentRequestDto
{
    public string merchant_id  { get; set; } 
    public string callback_url  { get; set; }
    public string? description { get; set; }
    public long amount { get; set; }
    public string? Mobile { get; set; }
    public string currency { get; set; } = "IRT";


}