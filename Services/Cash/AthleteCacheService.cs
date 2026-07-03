using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sport_app_backend.Data;
using sport_app_backend.Infrastructure.Cache;
using sport_app_backend.Models.Account.Athlete;

namespace sport_app_backend.Services.Cash;

public class AthleteCacheService(
    IMemoryCache cache,
    ApplicationDbContext context)
{


    private MemoryCacheEntryOptions CacheOptions =>
        new()
        {
            SlidingExpiration =
                TimeSpan.FromMinutes(30),

            AbsoluteExpirationRelativeToNow =
                TimeSpan.FromHours(2),

            Priority =
                CacheItemPriority.High
        };

    public async Task<Athlete?> GetAthleteByIdAsync(
        int athleteId)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.Athlete(athleteId),
            async entry =>
            {
                entry.SetOptions(CacheOptions);

                return await context.Athletes
                    .FirstOrDefaultAsync(a =>
                        a.Id == athleteId);
            });
    }

    public async Task<Athlete?> GetAthleteByPhoneAsync(
        string phoneNumber)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.AthletePhone(phoneNumber),
            async entry =>
            {
                entry.SetOptions(CacheOptions);

                return await context.Athletes
                    .FirstOrDefaultAsync(a =>
                        a.PhoneNumber == phoneNumber);
            });
    }

    public async Task<int> GetAthleteIdByPhoneAsync(
        string phoneNumber)
    {
        var athlete =
            await GetAthleteByPhoneAsync(phoneNumber);

        return athlete?.Id ?? 0;
    }

    public void UpdateAthlete(Athlete athlete)
    {
        cache.Set(
            CacheKeys.Athlete(athlete.Id),
            athlete,
            CacheOptions);

        if (!string.IsNullOrWhiteSpace(
                athlete.PhoneNumber))
        {
            cache.Set(
                CacheKeys.AthletePhone(
                    athlete.PhoneNumber),
                athlete,
                CacheOptions);
        }
    }

    public void RemoveAthlete(int athleteId)
    {
        cache.Remove(
            CacheKeys.Athlete(athleteId));
    }

    public void RemoveAthletePhone(
        string phoneNumber)
    {
        cache.Remove(
            CacheKeys.AthletePhone(phoneNumber));
    }

    public async Task RefreshAthleteAsync(
        int athleteId)
    {
        var athlete =
            await context.Athletes
                .FirstOrDefaultAsync(a =>
                    a.Id == athleteId);

        if (athlete == null)
            return;

        UpdateAthlete(athlete);
    }
}
