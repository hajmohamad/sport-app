namespace sport_app_backend.Dtos;

public class AthleteStatsDto
{
    public int ActiveAthletes { get; set; }
    public int InactiveAthletes { get; set; }
    public int NeedsFollowUp { get; set; }
    public int NearingCompletion { get; set; }
}