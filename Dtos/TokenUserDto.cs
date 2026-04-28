using sport_app_backend.Models.Account;

namespace sport_app_backend.Dtos;

public class TokenUserDto
{
    public int Id { get; set; }
    public TypeOfUser TypeOfUser { get; set; }
    public int? AthleteId { get; set; }
    public int? CoachId { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime LastLogin { get; set; }
}