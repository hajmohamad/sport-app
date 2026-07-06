using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Controller;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Repository.AthleteRepo;

public class ActivityRepository(
    ApplicationDbContext context,
     
    ITokenService tokenService,
    ICalculator calculator) : IActivity
{
            public async Task<ApiResponse> GetMonthlyActivity(string phoneNumber, int year, int month)
        {
            var athleteId = await context.Athletes
                .AsNoTracking()
                .Where(x => x.PhoneNumber == phoneNumber)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
            if (athleteId is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var persianCalendar = new System.Globalization.PersianCalendar();
            DateTime startDate;
            DateTime endDate;

            try
            {
                startDate = persianCalendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
                endDate = startDate.AddMonths(1);
            }
            catch (Exception ex)
            {
                return new ApiResponse() { Message = $"Error converting date: {ex.Message}", Action = false };
            }


            var activitiesDto = await context.Activities
                .AsNoTracking()
                .Where(x => x.AthleteId == athleteId && x.Date >= startDate && x.Date < endDate)
                .Select(x => new ActivityDto()
                {
                    Id = x.Id,
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    CaloriesLost = x.CaloriesLost,
                    Duration = x.Duration,
                    ActivityCategory = x.ActivityCategory.ToString(),
                    Name = x.Name ?? ""
                })
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Activities found",
                Action = true,
                Result = activitiesDto
            };
        }

        public async Task<ApiResponse> GetLastWeekActivity(string phoneNumber)
        {
            var athleteId = await context.Athletes
                .AsNoTracking()
                .Where(x => x.PhoneNumber == phoneNumber)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (athleteId is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }

            var lastSaturday = GetLastSaturday(DateTime.Now.Date);


            var activities = await context.Activities
                .AsNoTracking()
                .Where(x => x.AthleteId == athleteId && x.Date >= lastSaturday)
                .Select(x => new ActivityDto()
                {
                    Id = x.Id,
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    CaloriesLost = x.CaloriesLost,
                    Duration = x.Duration,
                    ActivityCategory = x.ActivityCategory.ToString(),
                    Name = x.Name ?? ""
                })
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Activities found",
                Action = true,
                Result = activities
            };
        }

        public async Task<ApiResponse> TodayActivityReport(string phoneNumber)
        {
            var athleteId = await context.Athletes
                .AsNoTracking()
                .Where(x => x.PhoneNumber == phoneNumber)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (athleteId is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }

            var today = DateTime.Now.Date;


            var activities = await context.Activities
                .AsNoTracking()
                .Where(x => x.AthleteId == athleteId && x.Date == today)
                .Select(x => new ActivityDto()
                {
                    Id = x.Id,
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    CaloriesLost = x.CaloriesLost,
                    Duration = x.Duration,
                    ActivityCategory = x.ActivityCategory.ToString(),
                    Name = x.Name ?? ""
                })
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Activities found",
                Action = true,
                Result = activities
            };
        }

        public async Task<ApiResponse> AddActivity(string phoneNumber, AddActivityDto addSportDto)
        {
            var athlete = await context.Athletes
                .Where(x => x.PhoneNumber == phoneNumber)
                .FirstOrDefaultAsync();

            if (athlete is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }

            if (!Enum.TryParse<ActivityCategory>(addSportDto.ActivityCategory, true, out var activityCategory))
            {
                return new ApiResponse() { Message = "Invalid activity category provided", Action = false };
            }

            var sport = new Activity()
            {
                AthleteId = athlete.Id,
                ActivityCategory = activityCategory,
                CaloriesLost = addSportDto.CaloriesLost,
                Distance = addSportDto.Distance,
                Duration = addSportDto.Duration,
                Date = Convert.ToDateTime(addSportDto.Date)
            };
            athlete.TotalActivities += 1;
            athlete.TotalCalories += addSportDto.CaloriesLost;

            await context.Activities.AddAsync(sport);
            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Message = "Sport added successfully",
                Action = true,
                Result = new
                {
                    sport.CaloriesLost,
                    sport.Duration,
                    sport.Distance,
                    ActivityCategory = sport.ActivityCategory.ToString()
                }
            };
        }



        public async Task<ApiResponse> DeleteActivity(string phoneNumber, int activityId)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete
            var activity =
                await context.Activities.FirstOrDefaultAsync(x => x.Id == activityId && x.AthleteId == athlete.Id);
            if (activity is null) return new ApiResponse() { Message = "Activity not found", Action = false };
            context.Activities.Remove(activity);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Activity deleted successfully",
                Action = true
            };
        }

public async Task<ApiResponse> GetActivityPage(string phoneNumber)
{
    var today = DateTime.Today;
    var lastSaturday = GetLastSaturday(today);
    var firstDayOfPersianMonth = GetFirstDayOfPersianMonth(today);

    var athleteData = await context.Athletes
        .AsNoTracking()
        .Where(a => a.PhoneNumber == phoneNumber)
        .Select(a => new
        {
            IsAthleteFound = true,
            AthleteId = a.Id,
            a.CurrentWeight,
            CompletedSessionCount = a.ActiveWorkoutProgram != null
                ? a.ActiveWorkoutProgram.CompletedSessionCount
                : 0,

            TotalSessionCount = a.ActiveWorkoutProgram != null
                ? a.ActiveWorkoutProgram.TotalSessionCount
                : 0,

            GoalWeight = a.WeightGoal,
            Name = a.User.FirstName + " " + a.User.LastName,
            a.Height,
            a.TotalActivities,
            a.TotalCalories,

            // دریافت ثبت‌های وزن ماه جاری برای پردازش در حافظه
            RawMonthlyEntries = a.WeightEntries
                .Where(w => w.CurrentDate >= firstDayOfPersianMonth && w.CurrentDate <= today)
                .OrderBy(w => w.CurrentDate)
                .Select(w => new { w.CurrentDate, w.Weight })
                .ToList(),

            LastWeekActivityDates = a.Activities
                .Where(act => act.Date >= lastSaturday && act.Date <= today)
                .Select(act => act.Date)
                .ToList()
        })
        .FirstOrDefaultAsync();

    if (athleteData is null || !athleteData.IsAthleteFound)
    {
        return new ApiResponse { Message = "Athlete not found", Action = false };
    }

    // ۲. دریافت آخرین وزن ثبت شده قبل از ماه جاری
    var lastWeightBeforeMonth = await context.WeightEntries
        .AsNoTracking()
        .Where(x => x.AthleteId == athleteData.AthleteId && x.CurrentDate < firstDayOfPersianMonth)
        .OrderByDescending(x => x.CurrentDate)
        .Select(x => (double?)x.Weight)
        .FirstOrDefaultAsync();

    var dailyWeights = new List<WeightReportDto>();
    double currentRunningWeight = lastWeightBeforeMonth ?? athleteData.CurrentWeight;

    int entryIndex = 0;
    for (var date = firstDayOfPersianMonth; date <= today; date = date.AddDays(1))
    {
        while (entryIndex < athleteData.RawMonthlyEntries.Count && 
               athleteData.RawMonthlyEntries[entryIndex].CurrentDate.Date == date.Date)
        {
            currentRunningWeight = athleteData.RawMonthlyEntries[entryIndex].Weight;
            entryIndex++;
        }

        dailyWeights.Add(new WeightReportDto
        {
            Date = date.ToString("yyyy-MM-dd"),
            Weight = currentRunningWeight
        });
    }

    // محاسبه بیت‌مپ فعالیت‌های هفته گذشته
    var lastWeekActivitiesBitmap = Enumerable.Range(0, 7)
        .Select(offset =>
        {
            var date = lastSaturday.AddDays(offset);
            return athleteData.LastWeekActivityDates.Any(d => d.Date == date) ? 1 : 0;
        })
        .ToList();

    // محاسبه BMI
    double? bmi = null;
    if (athleteData.Height > 0 && athleteData.CurrentWeight > 0)
    {
        var heightInMeters = athleteData.Height / 100.0;
        var tempBmi = athleteData.CurrentWeight / (heightInMeters * heightInMeters);

        if (double.IsFinite(tempBmi))
            bmi = tempBmi;
    }

    // محاسبه درصد پیشرفت برنامه ورزشی
    var progress = athleteData.TotalSessionCount > 0
        ? (int)((double)athleteData.CompletedSessionCount / athleteData.TotalSessionCount * 100)
        : 0;

    return new ApiResponse
    {
        Message = "Activities found",
        Action = true,
        Result = new ActivityPageDto()
        {
            Name = athleteData.Name,
            Height = athleteData.Height,
            TotalActivities = athleteData.TotalActivities,
            TotalCalories = athleteData.TotalCalories,
            LastWeekActivities = lastWeekActivitiesBitmap,
            CurrentWeight = athleteData.CurrentWeight,
            GoalWeight = athleteData.GoalWeight,
            LastMonthWeights = dailyWeights, // خروجی وزن اصلاح شده و پیوسته
            Bmi = bmi,
            Progress = progress
        }
    };
}
        private DateTime GetLastSaturday(DateTime today)
        {
            var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            return today.AddDays(-diff);
        }

        private DateTime GetFirstDayOfPersianMonth(DateTime date)
        {
            var pc = new PersianCalendar();
            var year = pc.GetYear(date);
            var month = pc.GetMonth(date);
            return pc.ToDateTime(year, month, 1, 0, 0, 0, 0);
        }

}