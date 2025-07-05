
using sport_app_backend.Dtos.ProgramDto;


namespace sport_app_backend.Dtos
{
    public class TrainingSessionDto
    {
        public int Id { get; set; }
        public required int ProgramInDayId { get; set; }
        public required TrainingSessionProgramInDayDto ProgramInDay { get; set; }
        public int DayNumber { get; set; }
        public string TrainingSessionStatus { get; set; } = string.Empty;
        public required int[] ExerciseCompletionBitmap { get; set; } 
        public double CaloriesLost { get; set; } 
    
    }
}