namespace sport_app_backend.Dtos;

public class CheckCodeRequestFromBuyFromSiteDto
{
    public required string PhoneNumber { get; set; }
    public required string Code { get; set; }
    public required int CoachServiceId { get; set; }
}