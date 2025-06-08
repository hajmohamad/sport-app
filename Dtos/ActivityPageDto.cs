namespace sport_app_backend.Dtos;

public class ActivityPageDto
{
    public int TotalActivities { get; set; }
    public double TotalTime { get; set; }
    public double TotalCalories { get; set; }
    public List<int> LastWeekActivities { get; set; }
    public int NumberOfCupsDrinked { get; set; }
    public int DailyCupOfWater { get; set; }
    public int Reminder { get; set; }
    public List<ActivityDto> TodayActivities { get; set; } = [];
    public double CurrentWeight { get; set; }
    public double GoalWeight { get; set; }
    public List<WeightReportDto> LastMonthWeights { get; set; }= [];
}