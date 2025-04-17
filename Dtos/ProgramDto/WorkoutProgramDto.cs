using sport_app_backend.Models.Program;

namespace sport_app_backend.Dtos.ProgramDto;

public class WorkoutProgramDto
{
    public int Week { get; set; }
    public required List<string> GeneralWarmUp { get; set; }
    public required string DedicatedWarmUp { get; set; }
    public required string ProgramLevel { get; set; }
    public required List<ProgramInDayDto> Days { get; set; }
    public required List<string> ProgramPriority { get; set; }
}