namespace sport_app_backend.Dtos;

public class AthleteStatusDto
{
    public int AthleteId { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string ProfileImageUrl { get; set; }
    public string Status { get; set; } // e.g., "فعال", "غیرفعال"
    public string Service { get; set; } // e.g., "کاهش حجم"
    public string LastWorkout { get; set; }
}