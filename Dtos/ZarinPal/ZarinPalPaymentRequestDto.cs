namespace sport_app_backend.Dtos.ZarinPal;

public class ZarinPalPaymentRequestDto
{
    public string merchant_id  { get; set; } = "ff39b97f-abef-43d4-adc6-ebc3033f3f3b";
    public string callback_url  { get; set; }
    public string? description { get; set; }
    public long amount { get; set; }
    public string? Mobile { get; set; }

}