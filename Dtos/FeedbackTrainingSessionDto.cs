namespace sport_app_backend.Dtos
{
    public class FeedbackTrainingSessionDto
    {
        public int TrainingSessionId { get; set; }
        public required string ExerciseFeeling { get; set; } = "Good";
    }
}
