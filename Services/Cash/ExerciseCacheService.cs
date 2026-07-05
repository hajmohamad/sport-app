using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sport_app_backend.Data;
using sport_app_backend.Infrastructure.Cache;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Services.Cash;

public class ExerciseCacheService(IMemoryCache cache, ApplicationDbContext context) : IExerciseCacheService
{
    public async Task<List<Exercise>> GetAllExercisesAsync()
    {
        return await cache.GetOrCreateAsync(CacheKeys.AllExercises, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(100);

            return await context.Exercises
                .AsNoTracking()
                .ToListAsync();
        }) ?? []; ;
    }

    public async Task<Dictionary<BaseCategory, List<int>>> GetCoachPinsAsync(int coachId)
    {
        var key = CacheKeys.CoachPins(coachId);

        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);

            var pins = await context.CoachPineExercises
                .Where(p => p.CoachId == coachId)
                .ToListAsync();

            return pins.ToDictionary(
                p => p.BaseCategory,
                p => p.ExerciseIds.ToList());
        }) ?? [];
    }

    public async Task<List<int>> GetLastWorkoutsForAthleteAsync(int coachId, int athleteId)
    {
        var key = CacheKeys.CoachLastWorkout(coachId);

        var allWorkoutsCache = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);
            var workouts = await context.LastWorkoutExercises
                .Where(w => w.CoachId == coachId)
                .ToListAsync();

            return workouts
                .GroupBy(w => w.AthleteId)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(w => w.ExerciseIds).ToList()
                );
        }) ?? [];

        if (allWorkoutsCache.TryGetValue(athleteId, out var exerciseIds))
        {
            return exerciseIds;
        }

        return new List<int>();
    }
 
    
    public async Task UpdateCoachPinsAsync(int coachId, BaseCategory category, int exerciseId)
    {
        var key = CacheKeys.CoachPins(coachId);

        var pins = await GetCoachPinsAsync(coachId);

        if (!pins.ContainsKey(category))
            pins[category] = new List<int>();

        if (!pins[category].Contains(exerciseId))
            pins[category].Add(exerciseId);

        cache.Set(key, pins);
    }

    public async Task RemoveCoachPinAsync(int coachId, int exerciseId)
    {
        var key = CacheKeys.CoachPins(coachId);

        var pins = await GetCoachPinsAsync(coachId);

        foreach (var category in pins)
        {
            category.Value.Remove(exerciseId);
        }

        cache.Set(key, pins);
    }
}