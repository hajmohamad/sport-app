using sport_app_backend.Models.Account;

namespace sport_app_backend.Dtos;

public class AddRoleResponse
{
   
    public string? RefreshToken { get; set; }
    public string? AccessToken { get; set; }

    public required string TypeOfUser { get; set; }
    public required string Gender { get; set; }
    public required bool Questions { get; set; }
}
