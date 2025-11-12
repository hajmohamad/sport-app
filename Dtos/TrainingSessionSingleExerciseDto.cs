using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Dtos;

public class TrainingSessionSingleExerciseDto
{
    public int Id { get; set; }
    public List<int> Reps { get; set; }
    public required string Description { get; set; }
    public  required string RepType { get; set; }
    public int ExerciseId { get; set; }
    public string ExercisePersianName { get; set; }
    public string ExerciseImage { get; set; }
}