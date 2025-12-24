using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Models;
using sport_app_backend.Models.Account.Athlete;

namespace sport_app_backend.Repository.AthleteRepo;

public class WaterAndWeightRepository(
    ApplicationDbContext context) : IWaterAndWeight
{
    public async Task<ApiResponse> AddWaterIntake(string phoneNumber, WaterInTakeDto waterInTakeDto)
    {
        if (waterInTakeDto.DailyCupOfWater < 0 || waterInTakeDto.Reminder < 0)
        {
            return new ApiResponse() { Message = "Input values cannot be negative", Action = false };
        }


        var athleteId = await context.Athletes
            .AsNoTracking()
            .Where(x => x.PhoneNumber == phoneNumber)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        if (athleteId is null)
        {
            return new ApiResponse() { Message = "User is not an athlete", Action = false };
        }


        var existingWaterIntake = await context.WaterInTakes
            .FirstOrDefaultAsync(w => w.AthleteId == athleteId.Value);


        if (existingWaterIntake != null)
        {
            existingWaterIntake.DailyCupOfWater = waterInTakeDto.DailyCupOfWater;
            existingWaterIntake.Reminder = waterInTakeDto.Reminder;
        }
        else
        {
            var newWaterIntake = new WaterInTake
            {
                AthleteId = athleteId.Value,
                DailyCupOfWater = waterInTakeDto.DailyCupOfWater,
                Reminder = waterInTakeDto.Reminder
            };
            context.WaterInTakes.Add(newWaterIntake);
        }

        await context.SaveChangesAsync();

        return new ApiResponse()
        {
            Message = "Water intake information saved successfully",
            Action = true
        };
    }

    

      public async Task<ApiResponse> UpdateWaterInDay(string phoneNumber, int numberOfCup)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not athlete", Action = false };
            var waterInDay = await context.WaterInDays
                .Where(w => w.AthleteId == athlete.Id && w.Date.Date == DateTime.Now.Date)
                .FirstOrDefaultAsync();

            if (waterInDay is not null)
            {
                waterInDay.NumberOfCupsDrinked += numberOfCup;
                context.WaterInDays.Update(waterInDay);
                await context.SaveChangesAsync();
                return new ApiResponse() { Message = "WaterInDay updated successfully", Action = true };
            }

            if (numberOfCup <= 0) return new ApiResponse() { Message = "WaterInDay is zero", Action = false };
            waterInDay = new WaterInDay
            {
                AthleteId = athlete.Id,
                Date = DateTime.Now.Date,
                Athlete = athlete,
                NumberOfCupsDrinked = numberOfCup
            };
            athlete.WaterInDays.Add(waterInDay);
            await context.WaterInDays.AddAsync(waterInDay);
            await context.SaveChangesAsync();
            return new ApiResponse() { Message = "WaterInDay added successfully", Action = true };
        }


        public async Task<ApiResponse> UpdateGoalWeight(string phoneNumber, double goalWeight)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete == null) return new ApiResponse() { Action = false, Message = "User is not an athlete" };
            athlete.WeightGoal = goalWeight;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Goal weight updated successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> UpdateWeight(string phoneNumber, double weight)
        {
            var athlete = await context.Athletes
                .Include(a => a.WeightEntries) // برای اطمینان از بارگذاری لیست وزن‌ها
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var today = DateTime.Today;

            athlete.CurrentWeight = weight;

            var weightEntry = athlete.WeightEntries
                .FirstOrDefault(x => x.CurrentDate.Date == today);

            if (weightEntry is null)
            {
                weightEntry = new WeightEntry()
                {
                    AthleteId = athlete.Id,
                    Athlete = athlete,
                    CurrentDate = today,
                    Weight = weight
                };
                await context.WeightEntries.AddAsync(weightEntry);
            }
            else
            {
                weightEntry.Weight = weight;
            }

            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Message = "Weight updated successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> UpdateHeightWeight(string phoneNumber, double weight, int hight)
        {
            var athlete = await context.Athletes
                .Include(a => a.WeightEntries) // برای اطمینان از بارگذاری لیست وزن‌ها
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            athlete.CurrentWeight = weight;
            athlete.Height = hight;

            var today = DateTime.Today;

            var weightEntry = athlete.WeightEntries
                .FirstOrDefault(x => x.CurrentDate.Date == today);

            if (weightEntry is null)
            {
                Console.WriteLine("**null");
                weightEntry = new WeightEntry()
                {
                    AthleteId = athlete.Id,
                    Athlete = athlete,
                    CurrentDate = today, // استفاده از متغیر today
                    Weight = weight
                };
                await context.WeightEntries.AddAsync(weightEntry);
            }
            else
            {
                weightEntry.Weight = weight;
            }

            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Message = "Weight and hight updated successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> GetLastMonthWeightReport(string phoneNumber)
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

            var pc = new PersianCalendar();
            var today = DateTime.Now.Date;
            var firstDayOfPersianMonth = pc.ToDateTime(pc.GetYear(today), pc.GetMonth(today), 1, 0, 0, 0, 0);


            var weightEntries = await context.WeightEntries
                .AsNoTracking()
                .Where(x => x.AthleteId == athleteId.Value && x.CurrentDate >= firstDayOfPersianMonth)
                .OrderByDescending(x => x.CurrentDate)
                .Select(x => new WeightReportDto()
                {
                    Date = x.CurrentDate.ToString("yyyy-MM-dd"),
                    Weight = x.Weight
                })
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Weight report fetched successfully",
                Action = true,
                Result = weightEntries
            };
        }

}