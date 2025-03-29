using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Controller;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Repository

{
    public class AthleteRepository(ApplicationDbContext context) : IAthleteRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<ApiResponse> ActivityReport(string phoneNumber)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var today = DateTime.Now.Date;
            var daysSinceSaturday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 6;
            var lastSaturday = today.AddDays(daysSinceSaturday);

            var activities = await _context.Activities
                .Where(x => x.AthleteId == athlete.Id && x.DateTime >= lastSaturday)
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Activities found",
                Action = true,
                Result = activities.Select(x => new
                {
                    x.Id,
                    Date = x.DateTime.ToString("yyyy-MM-dd"),
                    x.CaloriesLost,
                    x.Duration,
                    SportEnum = x.ActivityEnum.ToString(),
                    x.Name
                }).ToList()
            };
        }


        public async Task<ApiResponse> AddActivity(string phoneNumber, AddActivityDto addSportDto)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete

            var sportEnum = Enum.Parse<ActivitiesEnum>(addSportDto.ActivitiyEnum);

            var sport = new Activitie()
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                ActivityEnum = sportEnum,
                CaloriesLost = addSportDto.CaloriesLost,
                Distance = addSportDto.Distance,
                Duration = addSportDto.Duration,
                DateTime = DateTime.Now
            };

            await _context.Activities.AddAsync(sport);
            await _context.SaveChangesAsync();

            return new ApiResponse()
            {
                Message = "Sport added successfully",
                Action = true,
                Result = new
                {
                    sport.CaloriesLost,
                    sport.Duration,
                    sport.Distance,
                    SportEnum = sport.ActivityEnum.ToString()

                }
            };



        }

        public async Task<ApiResponse> AddWaterIntake(string phoneNumber, WaterInTakeDto waterInTakeDto)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            var waterIntake = new WaterInTake
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                DailyCupOfWater = waterInTakeDto.DailyCupOfWater,
                Reminder = waterInTakeDto.Reminder
            };
            athlete.WaterInTake = waterIntake;
            //edit last water intake if it exists
            var lastWaterIntake = await _context.WaterInTakes
                .Where(w => w.AthleteId == athlete.Id)
                .FirstOrDefaultAsync();
            if (lastWaterIntake != null)
            {
                lastWaterIntake.DailyCupOfWater = waterInTakeDto.DailyCupOfWater;
                lastWaterIntake.Reminder = waterInTakeDto.Reminder;
                _context.WaterInTakes.Update(lastWaterIntake);
                athlete.WaterInTake = lastWaterIntake; // Update the athlete's WaterInTake reference
            }
            else
            {
                athlete.WaterInTake = waterIntake;
                _context.WaterInTakes.Add(waterIntake);
            }
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "WaterIntake added successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> BuyCoachingPlan(string phoneNumber, int coachingPlanId)
        {
            var athlete = await _context.Athletes.Include(x=>x.AthleteQuestion).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            // if(athlete.AthleteQuestion is null) return new ApiResponse() { Message = "User has not completed the questions", Action = false };// Ensure the user is an athlete

            var coachingplan = await _context.CoachesPlan.Include(x=>x.Coach).FirstOrDefaultAsync(x => x.Id == coachingPlanId& x.IsActive==true);
            if (coachingplan is null) return new ApiResponse() { Message = "CoachingPlan not found", Action = false };
            if(coachingplan.IsDeleted==true) return new ApiResponse() { Message = "CoachingPlan is deleted", Action = false };
            var payment = new Payment()
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachPlan = coachingplan,
                CoachPlanId = coachingplan.Id,
                CoachId = coachingplan.CoachId,
                Coach = coachingplan.Coach,
                TransitionId = Guid.NewGuid().ToString(),
                Amount = coachingplan.Price,
            };
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Payment added successfully",
                Action = true,
                Result = new
                {
                    payment.Amount,
                    payment.TransitionId
                }
            };
            


        }

        public async Task<ApiResponse> SearchCoaches(CoachNameSearchDto coachNameSearchDto)
        {
            var coaches = await  _context.Users.Where(c=>(c.FirstName+" "+c.LastName).Contains(coachNameSearchDto.FullName)&&c.TypeOfUser==TypeOfUser.COACH).ToListAsync();
            return new ApiResponse()
            {
                Message = "Coaches found",
                Action = true,
                Result = coaches.Select(c => c.ToCoachForSearch()).ToList()
            };
        }


        public async Task<ApiResponse> DeleteActivity(string phoneNumber, int activityId)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            var activity = await _context.Activities.FirstOrDefaultAsync(x => x.Id == activityId && x.AthleteId == athlete.Id);
            if (activity is null) return new ApiResponse() { Message = "Activity not found", Action = false };
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Activity deleted successfully",
                Action = true

            };
        }

        public async Task<ApiResponse> SubmitAthleteQuestions(string phoneNumber, AthleteQuestionDto athleteQuestionDto)
        {
            var athlete = await _context.Athletes.Include(x => x.User).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            if (athlete.User is null) return new ApiResponse() { Message = "User not found", Action = false };

            ExerciseGoal exercisegoal;
            if (Enum.TryParse<ExerciseGoal>(athleteQuestionDto.ExerciseGoal, out var exerciseGoal))
                exercisegoal = exerciseGoal;
            else
                return new ApiResponse { Message = "Invalid ExerciseGoal value", Action = false };

            FitnessLevel fit;
            if (Enum.TryParse<FitnessLevel>(athleteQuestionDto.FitnessLevel, out var fitnessLevel))
                fit = fitnessLevel;
            else
                return new ApiResponse { Message = "Invalid FitnessLevel value", Action = false };

            var athleteQuestion = new AthleteQuestion
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                CurrentBodyForm = athleteQuestionDto.CurrentBodyForm,
                TargetBodyForm = athleteQuestionDto.TargetBodyForm,
                DaysPerWeekToExercise = athleteQuestionDto.DaysPerWeekToExercise,
                FitnessLevel = fit,
                InjuryArea = athleteQuestionDto.InjuryArea?.ToInjuryArea(),
                ExerciseGoal = exercisegoal,
            };
            athlete.AthleteQuestion = athleteQuestion;
            _context.AthleteQuestions.Add(athleteQuestion);

            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Athlete questions submitted successfully",
                Action = true
            };

        }

        public async Task <ApiResponse> AddHightAndWeight(string phoneNumber, HightWeightQuestionDto hightWeightQuestionDto)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            athlete.Height = hightWeightQuestionDto.Height;
            athlete.CurrentWeight = hightWeightQuestionDto.CurrentWeight;
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Height and weight added successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> UpdateWaterInDay(string phoneNumber)
        {
            var athlete = _context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not athlete", Action = false };
            var WaterInDay = await _context.WaterInDays
                .Where(w => w.AthleteId == athlete.Id && w.Date.Date == DateTime.Now.Date)
                .FirstOrDefaultAsync();
            if (WaterInDay is null)
            {
                var waterInDay = new WaterInDay
                {
                    AthleteId = athlete.Id,
                    Date = DateTime.Now.Date,
                    Athlete = athlete,
                    NumberOfCupsDrinked = 1 // Initialize with 1 cup since it's the first entry for today
                };
                athlete.WaterInDays.Add(waterInDay); // Add the new WaterInDay to the athlete's collection
                await _context.WaterInDays.AddAsync(waterInDay);
                await _context.SaveChangesAsync();
                return new ApiResponse() { Message = "WaterInDay added successfully", Action = true };
            }
            else
            {
                WaterInDay.NumberOfCupsDrinked += 1;
                _context.WaterInDays.Update(WaterInDay);
                await _context.SaveChangesAsync();
                return new ApiResponse() { Message = "WaterInDay updated successfully", Action = true };
            }
        }

        public async Task<ApiResponse> UpdateWeight(string phoneNumber, double weight)
        {

            var athlete = _context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            athlete.CurrentWeight = weight;
            var weightEntry = _context.WeightEntries.FirstOrDefault(x => x.AthleteId == athlete.Id && x.CurrentDate.Date == DateTime.Now.Date);
            if (weightEntry is null)
            {
                weightEntry = new WeightEntry()
                {
                    AthleteId = athlete.Id,
                    Athlete = athlete,
                    CurrentDate = DateTime.Now,
                    Weight = weight
                };
                await _context.WeightEntries.AddAsync(weightEntry);
                athlete.WeightEntries.Add(weightEntry);
                await _context.SaveChangesAsync();
            }
            else
            {
                weightEntry.Weight = weight;
                weightEntry.CurrentDate = DateTime.Now;
                await _context.SaveChangesAsync();

            }
            return new ApiResponse()
            {
                Message = "Weight updated successfully",
                Action = true
            };
        }


        public async Task<ApiResponse> WeightReport(string phoneNumber)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

           
            PersianCalendar pc = new PersianCalendar();
            DateTime today = DateTime.Now;
            int persianYear = pc.GetYear(today);
            int persianMonth = pc.GetMonth(today);
            DateTime firstDayOfPersianMonth = pc.ToDateTime(persianYear, persianMonth, 1, 0, 0, 0, 0);

            // دریافت رکوردهای وزن از اول ماه شمسی تاکنون
            var weightEntries = await _context.WeightEntries
                .Where(x => x.AthleteId == athlete.Id && x.CurrentDate >= firstDayOfPersianMonth)
                .OrderByDescending(x => x.CurrentDate)
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Weight report fetched successfully",
                Action = true,
                Result = weightEntries.Select(x => new
                {
                    Date = x.CurrentDate.ToString("yyyy-MM-dd"),
                    x.Weight
                }).ToList()
            };
        }
        



    }
}
