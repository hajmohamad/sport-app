using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Models.Program;

public class TrainingSession
{
    [Key]
    public int Id { get; set; }

    public required int WorkoutProgramId { get; set; }

    public required WorkoutProgram WorkoutProgram { get; set; }
    public required int ProgramInDayId { get; set; }
    public required ProgramInDay ProgramInDay { get; set; }
    public int DayNumber { get; set; }
    public TrainingSessionStatus TrainingSessionStatus { get; set; } = TrainingSessionStatus.NOTSTARTED;
    public ExerciseFeeling ExerciseFeeling { get; set; } = ExerciseFeeling.Good;
    public required byte[] ExerciseCompletionBitmap { get; set; } 
}