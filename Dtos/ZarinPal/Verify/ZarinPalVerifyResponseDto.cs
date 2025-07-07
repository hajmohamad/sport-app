namespace sport_app_backend.Dtos.ZarinPal.Verify;

public class ZarinPalVerifyResponseDto
{
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
    public long RefId { get; set; } 
}