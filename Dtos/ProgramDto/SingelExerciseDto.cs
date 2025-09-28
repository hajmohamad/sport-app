namespace sport_app_backend.Dtos.ProgramDto;

public class SingleExerciseDto
{
    public int Id { get; set; }
    public int Set { get; set; }
    public int Rep { get; set; }
    public int ExerciseId { get; set; }
    public string BaseCategory { get; set; } = "";
    public string PersianName { get; set; } = "";
}