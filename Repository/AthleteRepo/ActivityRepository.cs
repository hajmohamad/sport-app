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


            var activityPageData = await context.Athletes
                .AsNoTracking() 
                .Where(a => a.PhoneNumber == phoneNumber)
                .Select(a => new
                {
                    IsAthleteFound = true,
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
                    LastMonthWeights = a.WeightEntries
                        .Where(w => w.CurrentDate >= firstDayOfPersianMonth)
                        .OrderByDescending(w => w.CurrentDate)
                        .Select(w => new WeightReportDto
                        {
                            Date = w.CurrentDate.ToString("yyyy-MM-dd"),
                            Weight = w.Weight
                        }).ToList(),

                    LastWeekActivityDates = a.Activities
                        .Where(act => act.Date >= lastSaturday && act.Date <= today)
                        .Select(act => act.Date)
                        .ToList(),
                    // DailyCupOfWater = a.WaterInTake != null ? a.WaterInTake.DailyCupOfWater : 0,
                    // Reminder = a.WaterInTake != null ? a.WaterInTake.Reminder : 0,
                    // TotalTime = a.Activities.Sum(act => (double?)act.Duration) ?? 0,
                    // TodayActivities = a.Activities
                    //     .Where(act => act.Date == today)
                    //     .Select(act => new ActivityDto
                    //     {
                    //         Id = act.Id,
                    //         Date = act.Date.ToString("yyyy-MM-dd"),
                    //         CaloriesLost = act.CaloriesLost,
                    //         Duration = act.Duration,
                    //         ActivityCategory = act.ActivityCategory.ToString(),
                    //         Name = act.Name ?? ""
                    //     }).ToList(),
                    // NumberOfCupsDrinked = a.WaterInDays
                    //     .Where(w => w.Date == today)
                    //     .Select(w => w.NumberOfCupsDrinked)
                    //     .FirstOrDefault() // مقدار ۰ به صورت پیش‌فرض برمی‌گرداند
                })
                .FirstOrDefaultAsync();

            if (activityPageData is null || !activityPageData.IsAthleteFound)
            {
                return new ApiResponse { Message = "Athlete not found", Action = false };
            }

            var lastWeekActivitiesBitmap = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = lastSaturday.AddDays(offset);
                    return activityPageData.LastWeekActivityDates.Any(d => d.Date == date) ? 1 : 0;
                })
                .ToList();
            double? bmi = null;

            if (activityPageData.Height > 0 && activityPageData.CurrentWeight > 0)
            {
                var heightInMeters = activityPageData.Height / 100.0;
                var tempBmi = activityPageData.CurrentWeight / (heightInMeters * heightInMeters);

                if (double.IsFinite(tempBmi))
                    bmi = tempBmi;
            }
         
            var progress = activityPageData.TotalSessionCount > 0
                ? (int)((double)activityPageData.CompletedSessionCount / activityPageData.TotalSessionCount * 100)
                : 0;


         

            return new ApiResponse
            {
                Message = "Activities found",
                Action = true,
                Result = new ActivityPageDto()
                {
                    Name = activityPageData.Name,
                    Height = activityPageData.Height,
                    TotalActivities = activityPageData.TotalActivities,
                    TotalCalories = activityPageData.TotalCalories,
                    LastWeekActivities = lastWeekActivitiesBitmap,
                    CurrentWeight = activityPageData.CurrentWeight,
                    GoalWeight = activityPageData.GoalWeight,
                    LastMonthWeights = activityPageData.LastMonthWeights,
                    Bmi = bmi,
                    Progress = progress
                    // NumberOfCupsDrinked = activityPageData.NumberOfCupsDrinked,
                    // DailyCupOfWater = activityPageData.DailyCupOfWater,
                    // Reminder = activityPageData.Reminder,
                    // TodayActivities = activityPageData.TodayActivities,
                    // TotalTime = activityPageData.TotalTime,
                    
                   
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