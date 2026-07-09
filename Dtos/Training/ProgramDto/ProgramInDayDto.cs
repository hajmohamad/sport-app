using sport_app_backend.Models.Program;

namespace sport_app_backend.Dtos.ProgramDto;

public class ProgramInDayDto
{
    public int Id { get; set; }
    public int ForWhichDay { get; set; }
    public List<SingleExerciseDto> AllExerciseInDays { get; set; }= [];
}