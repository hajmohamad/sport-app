namespace sport_app_backend.Dtos.ProgramDto;

public class SingleExerciseDto
{
    public int Id { get; set; }
    public required List<int> Reps { get; set; }
    public required string RepType { get; set; }

    public string Description { get; set; } = "";
    public int ExerciseId { get; set; }
    public string BaseCategory { get; set; } = "";
    public string PersianName { get; set; } = "";
}