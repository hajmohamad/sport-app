namespace sport_app_backend.Dtos;

public class CheckCodeRequestDto
{
    public required string PhoneNumber { get; set; }
    public required string Code { get; set; }
    
}
