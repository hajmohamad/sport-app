using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Athlete;

public interface IWaterAndWeight
{
    public Task<ApiResponse> AddWaterIntake(string phoneNumber, WaterInTakeDto waterInTakeDto);
    public Task<ApiResponse> UpdateWaterInDay(string phoneNumber,int numberOfCup);
    public Task<ApiResponse> UpdateGoalWeight(string phoneNumber, double goalWeight);
    public Task<ApiResponse> UpdateWeight(string phoneNumber, double weight);
    public Task<ApiResponse> UpdateHeightWeight(string phoneNumber, double weight, int height);
    public Task<ApiResponse> GetLastMonthWeightReport(string phoneNumber);
}