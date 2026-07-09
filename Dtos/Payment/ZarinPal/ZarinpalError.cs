namespace sport_app_backend.Dtos;

public class ZarinpalError
{
    public int Code { get; set; }
    public string Message { get; set; }
    public List<string> Validations { get; set; } // برای خطاهای اعتبارسنجی

}