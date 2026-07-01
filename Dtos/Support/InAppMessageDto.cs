namespace sport_app_backend.Dtos;

public class InAppMessageDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ActionLink { get; set; }
    public string? ActionText { get; set; }
    public bool IsRead { get; set; }
    public string CreatedAt { get; set; } = "";
}