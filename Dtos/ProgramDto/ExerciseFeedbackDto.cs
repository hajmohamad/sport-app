namespace sport_app_backend.Dtos.ProgramDto
{
    public class ExerciseFeedbackDto
    {
        public int SingleExerciseId { get; set; }
        public bool IsPositive { get; set; }
        public string? NegativeReason { get; set; }
        public int TrainingSessionId { get; set; }

    }
}
