namespace sport_app_backend.Dtos;

public class FinishTrainingSessionDto
{
    
    public int TrainingSessionId { get; set; }
    public required string TrainingSessionName { get; set; }
    public double Duration { get; set; }
    public double CaloriesLost{get;set;}
}