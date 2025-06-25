namespace sport_app_backend.Dtos;

public class CoachDashboardDto
{
    public double TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public List<DailyIncomeDto> MonthlyIncome { get; set; } = [];
    public int TotalPrograms { get; set; }
    public int TotalAthletes { get; set; }
    public AthleteStatsDto AthleteStats { get; set; }
    public List<DailySessionCountDto> DailySessionCountChart { get; set; } = [];

}