namespace sport_app_backend.Dtos.ProgramDto
{
    public class ExerciseChangeDto
    {
        public int SingleExerciseId { get; set; }
        public string? Reason { get; set; }
        public int CoachId { get; set; }
        public int TrainingSessionId { get; set; }
    }
}
