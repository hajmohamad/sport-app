using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Models;
using sport_app_backend.Models.Challenge_Achievement;

namespace sport_app_backend.Repository.AthleteRepo;

public class AchievementsRepository(
    ApplicationDbContext context,
     
    ITokenService tokenService,
    ICalculator calculator) : IAchievements
{
            public async Task<ApiResponse> CompleteNewChallenge(string phoneNumber, string challenge)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var challengeExists =
                context.Challenges.Any(x => x.AthleteId == athlete.Id && x.ChallengeType.ToString() == challenge);
            if (challengeExists)
                return new ApiResponse() { Message = "Challenge already completed", Action = false };

            var chal = new Challenge
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                ChallengeType = Enum.Parse<ChallengeType>(challenge),
                CompletedAt = DateTime.Now
            };
            await context.Challenges.AddAsync(chal);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Challenge completed successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> CompletedChallenge(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var challenges = await context.Challenges
                .Where(x => x.AthleteId == athlete.Id)
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Challenges found",
                Action = true,
                Result = challenges.Select(c => c.ChallengeType.ToString()).ToList()
            };
        }

        public async Task<ApiResponse> GetAchievements(string phoneNumber)
        {
            var athlete = await context.Athletes.Include(athlete => athlete.WorkoutPrograms)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };


            var challenges = await context.Challenges.Where(c => c.AthleteId == athlete.Id).ToListAsync();
            var firstChallenge = challenges.Count != 0;
            var challengeSeeker = challenges.Count >= 5;
            var challengeMaster = challenges.Count >= 12;

            var activities = await context.Activities.Where(a => a.AthleteId == athlete.Id).ToListAsync();
            var firstWorkout = activities.Count != 0;
            var workoutDays = activities.Select(a => a.Date.Date).Distinct().OrderBy(d => d).ToList();
            var consistentAthlete = await HasConsecutiveDaysAsync(workoutDays, 7);
            var oneMonthComplete = workoutDays.Count >= 30;
            var threeMonthsGolden = workoutDays.Count >= 90;
            var masterAthlete = workoutDays.Count >= 365;
            //check 
            var firstProgramDone = athlete.WorkoutPrograms.Any(w => w.StartDate < DateTime.Now);
            //
            var achievements = new List<AchievementType>();
            if (firstWorkout)
                achievements.Add(AchievementType.FirstWorkout);
            if (consistentAthlete)
                achievements.Add(AchievementType.ConsistentAthlete);
            if (firstProgramDone)
                achievements.Add(AchievementType.FirstProgramDone);
            if (oneMonthComplete)
                achievements.Add(AchievementType.OneMonthComplete);
            if (threeMonthsGolden)
                achievements.Add(AchievementType.ThreeMonthsGolden);
            if (masterAthlete)
                achievements.Add(AchievementType.MasterAthlete);
            if (firstChallenge)
                achievements.Add(AchievementType.FirstChallenge);
            if (challengeSeeker)
                achievements.Add(AchievementType.ChallengeSeeker);
            if (challengeMaster)
                achievements.Add(AchievementType.ChallengeMaster);

            return new ApiResponse()
            {
                Message = "Achievements found",
                Action = true,
                Result = achievements.Select(a => a.ToString()).ToList()
            };
        }
        private static async Task<bool> HasConsecutiveDaysAsync(List<DateTime> dates, int requiredConsecutive)
        {
            return await Task.Run(() =>
            {
                if (dates.Count == 0)
                    return false;

                var consecutive = 1;
                for (var i = 1; i < dates.Count; i++)
                {
                    switch ((dates[i] - dates[i - 1]).Days)
                    {
                        case 1:
                        {
                            consecutive++;
                            if (consecutive >= requiredConsecutive)
                                return true;
                            break;
                        }
                        case > 1:
                            consecutive = 1;
                            break;
                    }
                }

                return false;
            });
        }


}