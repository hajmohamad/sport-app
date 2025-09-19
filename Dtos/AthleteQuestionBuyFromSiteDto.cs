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

}