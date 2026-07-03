namespace sport_app_backend.Dtos.Coach;

public class ProfileChecklistResponse
{
    public bool HasPersonalDetails { get; set; }
    public bool HasCommunicationChannels { get; set; }
    public bool HasWebsiteAddress { get; set; }
    public bool HasAthleteChange { get; set; }
    public bool HasUserReviews { get; set; }
}
