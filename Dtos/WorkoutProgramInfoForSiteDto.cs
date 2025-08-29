namespace sport_app_backend.Dtos;

public class WorkoutProgramInfoForSiteDto
{
    public string Status { get; set; }= "";
    public string WorkoutProgramPrice { get; set; } = "0";
    public string AthleteName { get; set; }= "";
    public string PaymentDate { get; set; }= "";
    public int ProgramDuration { get; set; }
    public string ProgramLevel { get; set; }= "";
    public List<string> ProgramPriorities { get; set; } = [];
    public string Title { get; set; } = "";
    public string CoachName { get; set; } = "";
    public CoachSocialMediaDto CoachSocialMedia { get; set; }
}
public  class CoachSocialMediaDto
{
    public string InstagramLink { get; set; }
    public string TelegramLink { get; set; }
    public string WhatsAppLink { get; set; }
}