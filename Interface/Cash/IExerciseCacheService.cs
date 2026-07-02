using sport_app_backend.Models;
using sport_app_backend.Models.Actions;

public interface IExerciseCacheService
{
    Task<List<Exercise>> GetAllExercisesAsync();

    Task<Dictionary<BaseCategory, List<int>>> GetCoachPinsAsync(int coachId);

    Task<Dictionary<int, List<int>>> GetCoachLastWorkoutsAsync(int coachId);

    Task UpdateCoachPinsAsync(int coachId, BaseCategory category, int exerciseId);

    Task RemoveCoachPinAsync(int coachId, int exerciseId);
}