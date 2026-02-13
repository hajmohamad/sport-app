using sport_app_backend.Models;

namespace sport_app_backend.Interface.Athlete;

public interface IAchievements
{
    public Task<ApiResponse> CompleteNewChallenge(string phoneNumber, string challenge);
    public Task<ApiResponse> CompletedChallenge(string phoneNumber);
    public Task<ApiResponse> GetAchievements(string phoneNumber);
}