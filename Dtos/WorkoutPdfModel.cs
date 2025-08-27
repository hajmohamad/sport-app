namespace sport_app_backend.Dtos;

public class WorkoutPdfModel
{
    public string ProgramTitle { get; set; }
    public string StartDate { get; set; }
    public string AthleteBmi { get; set; }
    public string AthleteWeight { get; set; }
    public string AthleteFatPercentage { get; set; }
    public string AthleteHeight { get; set; }
    public string CoachName { get; set; }
    public string ProgramLevel { get; set; }
    public string ProgramDuration { get; set; }
    public string ProgramPriorities { get; set; }
    public List<WorkoutDayModel> WorkoutDays { get; set; } = new();
}
public class WorkoutDayModel
{
    public int DayNumber { get; set; }
    public List<ExerciseModel> Exercises { get; set; } = new();
}

public class ExerciseModel
{
    public string Name { get; set; }
    public int Set { get; set; }
    public string Rep { get; set; }
}