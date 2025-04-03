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
using sport_app_backend.Models.Challenge_Achievement;
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
                    SportEnum = x.ActivityCategory.ToString(),
                    x.Name
                }).ToList()
            };
        }


        public async Task<ApiResponse> AddActivity(string phoneNumber, AddActivityDto addSportDto)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete

            var sportEnum = Enum.Parse<ActivityCategory>(addSportDto.ActivityCategory);

            var sport = new Activitie()
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                ActivityCategory = sportEnum,
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
                    ActivityCategory = sport.ActivityCategory.ToString()

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
            var athlete = await _context.Athletes.Include(x=>x.AthleteQuestions).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };
            if(athlete.AthleteQuestions.Count==0 ) return new ApiResponse() { Message = "User has not completed the questions", Action = false };
            var lastQuestion = athlete.AthleteQuestions?.LastOrDefault();

            var coachPlan = await _context.CoachesPlan.Include(x=>x.Coach).FirstOrDefaultAsync(x => x.Id == coachingPlanId& x.IsActive==true);
            if (coachPlan is null) return new ApiResponse() { Message = "CoachingPlan not found", Action = false };
            if(coachPlan.IsDeleted==true) return new ApiResponse() { Message = "CoachingPlan is deleted", Action = false };
            var payment = new Payment()
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachPlan = coachPlan,
                CoachPlanId = coachPlan.Id,
                CoachId = coachPlan.CoachId,
                Coach = coachPlan.Coach,
                TransitionId = Guid.NewGuid().ToString(),
                Amount = coachPlan.Price,
                AthleteQuestion = lastQuestion
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

        public async Task<ApiResponse> GetLastQuestion(string phoneNumber)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }
            var lastQuestion = _context.AthleteQuestions
                .Where(q => q.AthleteId == athlete.Id).Include(i=>i.InjuryArea)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefault();
            if (lastQuestion is null) return new ApiResponse() { Message = "Question not found", Action = false };
            
            return new ApiResponse()
            {
                Message = "Question found",
                Action = true,
                Result = lastQuestion.ToAthleteQuestionDto()
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

            var athleteQuestion = athleteQuestionDto.ToAthleteQuestion(athlete);
            athlete.AthleteQuestions.Add(athleteQuestion);
           await _context.AthleteQuestions.AddAsync(athleteQuestion);

            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Athlete questions submitted successfully",
                Action = true
            };

        }

        public async Task<ApiResponse> AthleteFirstQuestions(string phoneNumber, AthleteFirstQuestionsDto athleteFirstQuestionsDto) {
            var user = await _context.Users.Include(a=>a.Athlete).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
            var athlete=user.Athlete;
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            athlete.Height = athleteFirstQuestionsDto.Height;
            athlete.CurrentWeight = athleteFirstQuestionsDto.CurrentWeight;
            user.LastName=athleteFirstQuestionsDto.LastName;
            user.FirstName=athleteFirstQuestionsDto.FirstName;
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Athlete first questions submitted successfully",
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
        

        public async Task<ApiResponse> CompleteNewChallenge(string phoneNumber, string challenge)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var challengeExists = _context.Challenges.Any(x => x.AthleteId == athlete.Id && x.ChallengeType.ToString() == challenge);
            if (challengeExists)
                return new ApiResponse() { Message = "Challenge already completed", Action = false };

            var chal = new Challenge
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                ChallengeType = Enum.Parse<ChallengeType>(challenge),
                CompletedAt = DateTime.Now
            };
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Challenge completed successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> CompletedChallenge(string phoneNumber)
        {
            var athlete = await _context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var challenges = await _context.Challenges
                .Where(x => x.AthleteId == athlete.Id)
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Challenges found",
                Action = true,
                Result = challenges.Select(c => c.ChallengeType.ToString()).ToList()
            };
        }


    }
}
