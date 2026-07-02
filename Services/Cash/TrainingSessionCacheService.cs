using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sport_app_backend.Data;
using sport_app_backend.Infrastructure.Cache;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Services.Cash;

public class TrainingSessionCacheService
{
    private readonly IMemoryCache _cache;

    private readonly ApplicationDbContext _context;

    public TrainingSessionCacheService(
        IMemoryCache cache,
        ApplicationDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    private MemoryCacheEntryOptions CacheOptions =>
        new MemoryCacheEntryOptions
        {
            SlidingExpiration =
                TimeSpan.FromMinutes(20),

            AbsoluteExpirationRelativeToNow =
                TimeSpan.FromHours(2),

            Priority =
                CacheItemPriority.High
        };

    public async Task<TrainingSession?>
        GetTrainingSessionAsync(
            int trainingSessionId)
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.TrainingSession(
                trainingSessionId),
            async entry =>
            {
                entry.SetOptions(CacheOptions);

                return await _context.TrainingSessions
                    .Include(ts =>
                        ts.WorkoutProgram)

                    .Include(ts =>
                        ts.ProgramInDay)

                    .ThenInclude(p =>
                        p.AllExerciseInDays)

                    .ThenInclude(a =>
                        a.Exercise)

                    .FirstOrDefaultAsync(ts =>
                        ts.Id ==
                        trainingSessionId);
            });
    }

    public void RemoveTrainingSession(
        int trainingSessionId)
    {
        _cache.Remove(
            CacheKeys.TrainingSession(
                trainingSessionId));
    }
}