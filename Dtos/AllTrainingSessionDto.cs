

namespace sport_app_backend.Dtos;

public class AllTrainingSessionDto
{
    public int Id { get; set; }
    public int DayNumber { get; set; }
    public string TrainingSessionStatus { get; set; } = string.Empty;
    public required int[] ExerciseCompletionBitmap { get; set; } 
}