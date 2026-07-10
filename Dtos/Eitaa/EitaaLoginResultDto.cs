namespace sport_app_backend.Dtos.Eitaa;

public class EitaaLoginResultDto
{
    public bool Authenticated { get; set; }
    public bool RequiresContact { get; set; }
    public string? LinkingToken { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TypeOfUser { get; set; }
    public string? Gender { get; set; }
    public bool Questions { get; set; }
    public EitaaAuthenticatedUserDto? User { get; set; }
}
public class EitaaAuthenticatedUserDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? UserName { get; set; }
}