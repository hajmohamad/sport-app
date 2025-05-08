using sport_app_backend.Models.Actions;

namespace sport_app_backend.Dtos;

public class TrainingSessionSingleExerciseDto
{
    public int Id { get; set; }
    public int Set { get; set; }
    public int Rep { get; set; }
    public int ExerciseId { get; set; }  
    public  ExerciseDto? Exercise { get; set; }  
    
}