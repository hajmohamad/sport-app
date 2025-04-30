using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Dtos;

public class TrainingSessionProgramInDayDto
{
    public int Id { get; set; }
    public int ForWhichDay { get; set; }
    public List<TrainingSessionSingleExerciseDto> AllExerciseInDays { get; set; }= [];
}