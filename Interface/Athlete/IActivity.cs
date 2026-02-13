using sport_app_backend.Controller;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IActivity
{
    public Task<ApiResponse> AddActivity(string phoneNumber, AddActivityDto addSportDto);
    public Task<ApiResponse> DeleteActivity(string phoneNumber,int activityId);
    public Task<ApiResponse> GetLastWeekActivity(string phoneNumber);
    public Task<ApiResponse> TodayActivityReport(string phoneNumber);
    public Task<ApiResponse> GetMonthlyActivity(string phoneNumber, int year, int month);
    public Task<ApiResponse> GetActivityPage(string phoneNumber);

}