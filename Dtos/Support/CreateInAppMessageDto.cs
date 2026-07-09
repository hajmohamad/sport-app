namespace sport_app_backend.Dtos;

public class CreateInAppMessageDto
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? ActionLink { get; set; }
    public string? ActionText { get; set; }
    public required string TargetRole { get; set; } // "ATHLETE" یا "COACH"
}