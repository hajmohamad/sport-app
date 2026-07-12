namespace sport_app_backend.Dtos.Coach;

public class CoachBuyRequestDto {
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Gender { get; set; }
    public required string AthletePhoneNumber { get; set; }
    public int ServiceId { get; set; }
}