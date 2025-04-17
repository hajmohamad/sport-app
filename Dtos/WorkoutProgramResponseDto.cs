using System.ComponentModel.DataAnnotations;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Dtos;

public class WorkoutProgramResponseDto
{ 
    public int Id { get; set; }
    public string Title { get; set; }="";
    public int PaymentId { get; set; }
    public DateTime StartDate { get; set; }
    public int ProgramDuration { get; set; }
    public string ProgramLevel { get; set; }=string.Empty;
    public  List<string> ProgramPriorities { get; set; } = [];
    public List<string> GeneralWarmUp { get; set; } = [];
    public  string DedicatedWarmUp { get; set; } = "";
    public DateTime EndDate { get; set; }
    public string Status { get; set; }= "";
    public int Duration { get; set; }
    public string Description { get; set; } = "";
    public List<ProgramInDayDto>? ProgramInDays { get; set; } = [];
}