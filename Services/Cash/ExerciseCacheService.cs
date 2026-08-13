using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using sport_app_backend.Data;
using sport_app_backend.Infrastructure.Cache;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Services.Cash;

public class ExerciseCacheService(
    IMemoryCache cache,
    ApplicationDbContext context,
    IOptions<ExerciseCacheOptions> cacheOptions,
    ILogger<ExerciseCacheService> logger)
    : IExerciseCacheService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new();

    private readonly ExerciseCacheOptions _options = cacheOptions.Value;

    public async Task<List<Exercise>> GetAllExercisesAsync()
    {
        return await GetOrCreateAsync(
            CacheKeys.AllExercises,
            GetEntryOptions(GetPositiveMinutes(_options.AllExercisesTtlMinutes, 720)),
            async () => await context.Exercises
                .AsNoTracking()
                .ToListAsync(),
            "all exercises") ?? [];
    }

    public async Task<Dictionary<BaseCategory, List<int>>> GetCoachPinsAsync(int coachId)
    {
        var key = CacheKeys.CoachPins(coachId);

        return await GetOrCreateAsync(
            key,
            GetEntryOptions(GetPositiveMinutes(_options.CoachPinsTtlMinutes, 60)),
            async () =>
            {
                var pins = await context.CoachPineExercises
                    .AsNoTracking()
                    .Where(p => p.CoachId == coachId)
                    .ToListAsync();

                return pins
                    .GroupBy(p => p.BaseCategory)
                    .ToDictionary(
                        g => g.Key,
                        g => g.SelectMany(x => x.ExerciseIds).Distinct().ToList());
            },
            $"coach pins for coach {coachId}") ?? [];
    }

    public async Task<List<int>> GetLastWorkoutsForAthleteAsync(int coachId, int athleteId)
    {
        var key = CacheKeys.CoachLastWorkout(coachId, athleteId);

        return await GetOrCreateAsync(
            key,
            GetEntryOptions(GetPositiveMinutes(_options.LastWorkoutsTtlMinutes, 10)),
            async () =>
            {
                var workout = await context.LastWorkoutExercises
                    .AsNoTracking()
                    .Where(w => w.CoachId == coachId && w.AthleteId == athleteId)
                    .OrderByDescending(w => w.Id)
                    .FirstOrDefaultAsync();

                return workout?.ExerciseIds?.ToList() ?? [];
            },
            $"last workouts for coach {coachId}, athlete {athleteId}") ?? [];
    }

    public async Task UpdateCoachPinsAsync(int coachId, BaseCategory category, int exerciseId)
    {
        try
        {
            var key = CacheKeys.CoachPins(coachId);
            var pins = await GetCoachPinsAsync(coachId);

            if (!pins.TryGetValue(category, out var categoryPins))
            {
                categoryPins = [];
                pins[category] = categoryPins;
            }

            if (!categoryPins.Contains(exerciseId))
            {
                categoryPins.Add(exerciseId);
            }

            cache.Set(key, pins, GetEntryOptions(GetPositiveMinutes(_options.CoachPinsTtlMinutes, 60)));
            logger.LogInformation("Exercise cache updated: coach pins for coach {CoachId}", coachId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Exercise cache update failed for coach pins (coachId: {CoachId})",
                coachId);
        }
    }

    public async Task RemoveCoachPinAsync(int coachId, int exerciseId)
    {
        try
        {
            var key = CacheKeys.CoachPins(coachId);
            var pins = await GetCoachPinsAsync(coachId);

            foreach (var category in pins.Keys.ToList())
            {
                pins[category].Remove(exerciseId);
                if (pins[category].Count == 0)
                {
                    pins.Remove(category);
                }
            }

            cache.Set(key, pins, GetEntryOptions(GetPositiveMinutes(_options.CoachPinsTtlMinutes, 60)));
            logger.LogInformation(
                "Exercise cache updated: removed pin {ExerciseId} for coach {CoachId}",
                exerciseId,
                coachId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Exercise cache update failed while removing pin {ExerciseId} for coach {CoachId}",
                exerciseId,
                coachId);
        }
    }

    public Task InvalidateAllExercisesAsync()
    {
        TryRemove(CacheKeys.AllExercises, "all exercises");
        return Task.CompletedTask;
    }

    public Task InvalidateCoachPinsAsync(int coachId)
    {
        TryRemove(CacheKeys.CoachPins(coachId), $"coach pins for coach {coachId}");
        return Task.CompletedTask;
    }

    public Task InvalidateLastWorkoutsAsync(int coachId, int athleteId)
    {
        TryRemove(
            CacheKeys.CoachLastWorkout(coachId, athleteId),
            $"last workouts for coach {coachId}, athlete {athleteId}");
        return Task.CompletedTask;
    }

    private async Task<T?> GetOrCreateAsync<T>(
        string key,
        MemoryCacheEntryOptions entryOptions,
        Func<Task<T>> dbFactory,
        string cacheName)
    {
        if (TryGetValue(key, out T? cachedValue))
        {
            logger.LogDebug("Exercise cache hit: {CacheName}", cacheName);
            return cachedValue;
        }

        logger.LogDebug("Exercise cache miss: {CacheName}", cacheName);

        var keyLock = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync();

        try
        {
            if (TryGetValue(key, out cachedValue))
            {
                logger.LogDebug("Exercise cache hit after lock: {CacheName}", cacheName);
                return cachedValue;
            }

            var data = await dbFactory();

            try
            {
                cache.Set(key, data, entryOptions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Exercise cache set failed for {CacheName}; returning DB data",
                    cacheName);
            }

            return data;
        }
        finally
        {
            keyLock.Release();
        }
    }

    private bool TryGetValue<T>(string key, out T? value)
    {
        try
        {
            if (cache.TryGetValue(key, out T? cached) && cached is not null)
            {
                value = cached;
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exercise cache read failed for key {CacheKey}", key);
        }

        value = default;
        return false;
    }

    private void TryRemove(string key, string cacheName)
    {
        try
        {
            cache.Remove(key);
            logger.LogInformation("Exercise cache invalidated: {CacheName}", cacheName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Exercise cache invalidation failed for {CacheName}",
                cacheName);
        }
    }

    private static MemoryCacheEntryOptions GetEntryOptions(int ttlMinutes)
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlMinutes),
            Priority = CacheItemPriority.High
        };
    }

    private static int GetPositiveMinutes(int value, int fallback)
        => value > 0 ? value : fallback;
}
