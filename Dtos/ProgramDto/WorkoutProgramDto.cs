using sport_app_backend.Models.Program;

namespace sport_app_backend.Dtos.ProgramDto;

public class WorkoutProgramDto
{
    public bool Publish { get; set; } = false;
    public int Week { get; set; }
    // public List<string>? GeneralWarmUp { get; set; } = [];
    // public  string? DedicatedWarmUp { get; set; } 
    public required string ProgramLevel { get; set; }
    public required List<ProgramInDayDto> Days { get; set; }
    public required List<string> ProgramPriority { get; set; }
}