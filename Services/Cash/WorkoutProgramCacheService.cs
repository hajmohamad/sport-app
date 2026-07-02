using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sport_app_backend.Data;
using sport_app_backend.Infrastructure.Cache;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Services.Cash;

public class WorkoutProgramCacheService(
    IMemoryCache cache,
    ApplicationDbContext context)
{
    private static MemoryCacheEntryOptions CacheOptions =>
        new MemoryCacheEntryOptions
        {
            SlidingExpiration =
                TimeSpan.FromMinutes(20),

            AbsoluteExpirationRelativeToNow =
                TimeSpan.FromHours(4),

            Priority =
                CacheItemPriority.High
        };

    public async Task<WorkoutProgram?>
        GetActiveWorkoutProgramAsync(
            int athleteId)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.ActiveWorkoutProgram(
                athleteId),
            async entry =>
            {
                entry.SetOptions(CacheOptions);

                return await context.WorkoutPrograms
                    .AsNoTracking()
                    .Include(w => w.Coach)
                    .Include(w => w.TrainingSessions)
                    .FirstOrDefaultAsync(w =>
                        w.AthleteId == athleteId &&
                        w.Status ==
                        WorkoutProgramStatus.ACTIVE);
            });
    }

    public void RemoveActiveWorkoutProgram(
        int athleteId)
    {
        cache.Remove(
            CacheKeys.ActiveWorkoutProgram(
                athleteId));
    }
}