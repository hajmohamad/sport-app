using sport_app_backend.Dtos.ProgramDto;

namespace sport_app_backend.Dtos.WorkoutProgramTemplateDto;

public class WorkoutProgramTemplateCreateDto
{
    public string Title { get; set; } = "";
}

public class WorkoutProgramTemplateUpdateDto
{
    public string Description { get; set; } = "";
    public int ProgramDuration { get; set; }
    public string ProgramLevel { get; set; } = "";
    public string ProgramPriorities { get; set; } 
    public bool IsCompleted { get; set; } = false;
    public List<ProgramInDayDto> Days { get; set; } = [];
}
public class WorkoutProgramTemplateListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int ProgramDuration { get; set; }
    public string ProgramLevel { get; set; } = "";
    public string ProgramPriorities { get; set; } 
    public bool IsCompleted { get; set; }
    public int DaysCount { get; set; }
}

public class WorkoutProgramTemplateDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int ProgramDuration { get; set; }
    public string ProgramLevel { get; set; } = "";
    public string ProgramPriorities { get; set; } 
    public bool IsCompleted { get; set; }
    public List<ProgramInDayDto> Days { get; set; } 
}

public class CreateWorkoutProgramFromTemplateDto
{
    public int TemplateId { get; set; }
    public int PaymentId { get; set; }
}