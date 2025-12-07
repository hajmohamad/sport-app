namespace sport_app_backend.Dtos;

public class AthleteQuestionBuyFromSiteDto
{
    public required string WpKey { get; set; }
    public int DaysPerWeekToExercise { get; set; }
    public int CurrentBodyForm { get; set; }
    public double CurrentWeight { get; set; }
    public string? ExerciseGoal { get; set; }
    public InjuryAreaDto? InjuryArea { get; set; }
    public string? FitnessLevel { get; set; }
    public required string? BirthDay { get; set; }
    public required string Gender { get; set; }
    public int Height { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int AthleteBodyImageId { get; set; }
    public string ExerciseLocation { get; set; } = "";
    public string ComingCompetition  { get; set; }= "";
    public string CompetitionHistory { get; set; }= "";
    public string CurrentMedications {get; set;}= "";
    public string SittingHour {get; set;}= "";
    public string YourJob {get; set;}= "";
    public string YourCity {get; set;}= "";
}