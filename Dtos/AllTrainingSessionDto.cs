

namespace sport_app_backend.Dtos;

public class AllTrainingSessionDto
{
    public int Id { get; set; }
    public int DayNumber { get; set; }
    public string TrainingSessionStatus { get; set; } 
    public required byte[] ExerciseCompletionBitmap { get; set; } 
}