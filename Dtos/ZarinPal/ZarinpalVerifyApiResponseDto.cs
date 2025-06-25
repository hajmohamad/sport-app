namespace sport_app_backend.Dtos;

public class ZarinpalVerifyApiResponseDto
{
    public ZarinpalVerifyData Data { get; set; }
    public List<ZarinpalError> Errors { get; set; }
}

