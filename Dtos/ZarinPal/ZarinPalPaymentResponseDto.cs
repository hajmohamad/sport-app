namespace sport_app_backend.Dtos.ZarinPal;

public class ZarinPalPaymentResponseDto
{
   
    public bool IsSuccessful { get; set; }
    public string PaymentUrl { get; set; }
    public string Authority { get; set; }
    public string ErrorMessage { get; set; }
}