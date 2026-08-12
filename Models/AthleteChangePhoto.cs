using sport_app_backend.Models.Account.Coach;

namespace sport_app_backend.Models;
public class AthleteChangePhoto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // روابط (اگر لازم داری)
    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;
}
