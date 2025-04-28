
using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Controller;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Challenge_Achievement;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Repository

{
    public class AthleteRepository(ApplicationDbContext context) : IAthleteRepository
    {
        public async Task<ApiResponse> ActivityReport(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var today = DateTime.Now.Date;
            var daysSinceSaturday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 6;
            var lastSaturday = today.AddDays(daysSinceSaturday);

            var activities = await context.Activities
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
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete

            var sportEnum = Enum.Parse<ActivityCategory>(addSportDto.ActivityCategory!);

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

        public async Task<ApiResponse> AddWaterIntake(string phoneNumber, WaterInTakeDto waterInTakeDto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
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
            var lastWaterIntake = await context.WaterInTakes
                .Where(w => w.AthleteId == athlete.Id)
                .FirstOrDefaultAsync();
            if (lastWaterIntake != null)
            {
                lastWaterIntake.DailyCupOfWater = waterInTakeDto.DailyCupOfWater;
                lastWaterIntake.Reminder = waterInTakeDto.Reminder;
                context.WaterInTakes.Update(lastWaterIntake);
                athlete.WaterInTake = lastWaterIntake; // Update the athlete's WaterInTake reference
            }
            else
            {
                athlete.WaterInTake = waterIntake;
                context.WaterInTakes.Add(waterIntake);
            }
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "WaterIntake added successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> BuyCoachingService(string phoneNumber, int coachingServiceId)
        {
            var athlete = await context.Athletes.Include(x=>x.AthleteQuestions).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };
            if(athlete.AthleteQuestions.Count==0 ) return new ApiResponse() { Message = "User has not completed the questions", Action = false };
            var lastQuestion = athlete.AthleteQuestions.LastOrDefault();
            if(lastQuestion is null ) return new ApiResponse() { Message = "User has not completed the questions", Action = false };
            var coachService = await context.CoachServices.Include(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == coachingServiceId & x.IsActive == true);
            if (coachService is null) return new ApiResponse() { Message = "CoachingService not found", Action = false };
            if(coachService.IsDeleted) return new ApiResponse() { Message = "CoachingService is deleted", Action = false };
            var payment = new Payment()
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id,
                CoachId = coachService.CoachId,
                Coach = coachService.Coach,
                TransactionId = Guid.NewGuid().ToString(),
                Amount = coachService.Price,
                AthleteQuestion = lastQuestion
            };
            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Payment added successfully",
                Action = true,
                Result = new
                {
                    payment.Amount,
                    payment.TransactionId
                }
            };
            


        }

        public async Task<ApiResponse> SearchCoaches(CoachNameSearchDto coachNameSearchDto)
        {
            var coaches = await  context.Users.Where(c=>(c.FirstName+" "+c.LastName).Contains(coachNameSearchDto.FullName)&&c.TypeOfUser==TypeOfUser.COACH).ToListAsync();
            return new ApiResponse()
            {
                Message = "Coaches found",
                Action = true,
                Result = coaches.Select(c => c.ToCoachForSearch()).ToList()
            };
        }

        public async Task<ApiResponse> GetLastQuestion(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }
            var lastQuestion = context.AthleteQuestions
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
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            var activity = await context.Activities.FirstOrDefaultAsync(x => x.Id == activityId && x.AthleteId == athlete.Id);
            if (activity is null) return new ApiResponse() { Message = "Activity not found", Action = false };
            context.Activities.Remove(activity);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Activity deleted successfully",
                Action = true

            };
        }

        public async Task<ApiResponse> SubmitAthleteQuestions(string phoneNumber, AthleteQuestionDto athleteQuestionDto)
        {
            var athlete = await context.Athletes.Include(x => x.User).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            if (athlete.User is null) return new ApiResponse() { Message = "User not found", Action = false };
            athlete.User.BirthDate = Convert.ToDateTime(athleteQuestionDto.BirthDay);
            var athleteQuestion = athleteQuestionDto.ToAthleteQuestion(athlete);
            athlete.AthleteQuestions.Add(athleteQuestion);
           await context.AthleteQuestions.AddAsync(athleteQuestion);

            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Athlete questions submitted successfully",
                Action = true
            };

        }

        public async Task<ApiResponse> AthleteFirstQuestions(string phoneNumber, AthleteFirstQuestionsDto athleteFirstQuestionsDto) {
            var user = await context.Users.Include(a=>a.Athlete).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
            var athlete=user.Athlete;
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            athlete.Height = athleteFirstQuestionsDto.Height;
            athlete.CurrentWeight = athleteFirstQuestionsDto.CurrentWeight;
            user.LastName=athleteFirstQuestionsDto.LastName;
            user.FirstName=athleteFirstQuestionsDto.FirstName;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Athlete first questions submitted successfully",
                Action = true,
                Result = new
                {
                    Questions = true
                }
            };
        }

        public async Task<ApiResponse> UpdateWaterInDay(string phoneNumber)
        {
            var athlete = context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not athlete", Action = false };
            var waterInDay = await context.WaterInDays
                .Where(w => w.AthleteId == athlete.Id && w.Date.Date == DateTime.Now.Date)
                .FirstOrDefaultAsync();
            if (waterInDay is null)
            {
                 waterInDay = new WaterInDay
                {
                    AthleteId = athlete.Id,
                    Date = DateTime.Now.Date,
                    Athlete = athlete,
                    NumberOfCupsDrinked = 1 // Initialize with 1 cup since it's the first entry for today
                };
                athlete.WaterInDays.Add(waterInDay); // Add the new WaterInDay to the athlete's collection
                await context.WaterInDays.AddAsync(waterInDay);
                await context.SaveChangesAsync();
                return new ApiResponse() { Message = "WaterInDay added successfully", Action = true };
            }
            else
            {
                waterInDay.NumberOfCupsDrinked += 1;
                context.WaterInDays.Update(waterInDay);
                await context.SaveChangesAsync();
                return new ApiResponse() { Message = "WaterInDay updated successfully", Action = true };
            }
        }

        public async Task<ApiResponse> UpdateWeight(string phoneNumber, double weight)
        {

            var athlete = context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "User is not an athlete", Action = false };// Ensure the user is an athlete
            athlete.CurrentWeight = weight;
            var weightEntry = context.WeightEntries.FirstOrDefault(x => x.AthleteId == athlete.Id && x.CurrentDate.Date == DateTime.Now.Date);
            if (weightEntry is null)
            {
                weightEntry = new WeightEntry()
                {
                    AthleteId = athlete.Id,
                    Athlete = athlete,
                    CurrentDate = DateTime.Now,
                    Weight = weight
                };
                await context.WeightEntries.AddAsync(weightEntry);
                athlete.WeightEntries.Add(weightEntry);
                await context.SaveChangesAsync();
            }
            else
            {
                weightEntry.Weight = weight;
                weightEntry.CurrentDate = DateTime.Now;
                await context.SaveChangesAsync();

            }
            return new ApiResponse()
            {
                Message = "Weight updated successfully",
                Action = true
            };
        }


        public async Task<ApiResponse> WeightReport(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

           
            PersianCalendar pc = new PersianCalendar();
            DateTime today = DateTime.Now;
            int persianYear = pc.GetYear(today);
            int persianMonth = pc.GetMonth(today);
            DateTime firstDayOfPersianMonth = pc.ToDateTime(persianYear, persianMonth, 1, 0, 0, 0, 0);

            // دریافت رکوردهای وزن از اول ماه شمسی تاکنون
            var weightEntries = await context.WeightEntries
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
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var challengeExists = context.Challenges.Any(x => x.AthleteId == athlete.Id && x.ChallengeType.ToString() == challenge);
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
            var athlete = await context.Athletes.Include(athlete => athlete.WorkoutPrograms).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            

            var challenges = await context.Challenges.Where(c => c.AthleteId == athlete.Id).ToListAsync();
            var firstChallenge = challenges.Count != 0;
            var challengeSeeker = challenges.Count >= 5;
            var challengeMaster = challenges.Count >= 12;

            var activities = await context.Activities.Where(a => a.AthleteId == athlete.Id).ToListAsync();
            var firstWorkout = activities.Count != 0;
            var workoutDays = activities.Select(a => a.DateTime.Date).Distinct().OrderBy(d => d).ToList();
            var consistentAthlete = HasConsecutiveDays(workoutDays, 7);
            var oneMonthComplete = workoutDays.Count >= 30;
            var threeMonthsGolden = workoutDays.Count >= 90;
            var masterAthlete = workoutDays.Count >= 365;
            var firstProgramDone = athlete.WorkoutPrograms.Any(w => w.EndDate > DateTime.Now);

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

        public async Task<ApiResponse> GetAllPayments(string phoneNumber)
        {
            var athlete = await context.Athletes.Include(a => a.WorkoutPrograms)
                .ThenInclude(p=>p.Payment)
                .ThenInclude(s=>s.CoachService)
                .ThenInclude(x=>x.Coach)
                .ThenInclude(u=>u.User)
                .FirstOrDefaultAsync(z => z.PhoneNumber == phoneNumber);
            if(athlete is null)
                return new ApiResponse()
                    { Action = false, Message = "Athlete not found" };
            
            
            return new ApiResponse()
            {
                Action = true,
                Message = "Programs found",
                Result = athlete.WorkoutPrograms.Select(x => x.ToAllWorkoutProgramResponseDto()).ToList()
            };
            
        }

        public async Task<ApiResponse> GetPayment(string phoneNumber, int paymentId)
        {
            var payment = await context.Payments 
                .Include(p => p.Athlete)
                .ThenInclude(a => a.User)
                .Include(x => x.Coach)  // بارگذاری Coac
                .ThenInclude(a => a.User)
                .Include(a=>a.AthleteQuestion)// بارگذاری User داخل Athlete
                .ThenInclude(I=> I!.InjuryArea)
                .Include(w=>w.WorkoutProgram)
                .ThenInclude(z=>z.ProgramInDays)
                .ThenInclude(z=>z.AllExerciseInDays)
                .ThenInclude(e=>e.Exercise)
                .FirstOrDefaultAsync(p => p.Athlete.PhoneNumber == phoneNumber&& p.Id==paymentId);
            if(payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
            return new ApiResponse()
            {
                Message = "Payment found",
                Action = true,
                Result = payment.ToAthletePaymentResponseDto()
            };
        }

        public Task<ApiResponse> GetAllPrograms(string phoneNumber)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse> GetProgram(string phoneNumber, int programId)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse> ActiveProgram(string phoneNumber, int programId)
        {
            var athlete = await context.Athletes.Include(a => a.WorkoutPrograms)
                .FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
            if(athlete is null)
                return new ApiResponse()
                    { Action = false, Message = "Athlete not found" };


            foreach (var program in athlete.WorkoutPrograms.Where(x => x.Status == WorkoutProgramStatus.ACTIVE))
            {
                program.Status = WorkoutProgramStatus.STOPPED;
            }

            var workoutProgram = athlete.WorkoutPrograms.Find(w => w.Id == programId);
            if (workoutProgram is null)
            {
                return new ApiResponse()
                    { Action = false, Message = "workout program not found" };

            }
            workoutProgram.Status = WorkoutProgramStatus.ACTIVE;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "Program found",
            };
        }
            

        private static bool HasConsecutiveDays(List<DateTime> dates, int requiredConsecutive)
        {
            if (dates.Count == 0)
                return false;

            var consecutive = 1;
            for (var i = 1; i < dates.Count; i++)
            {
                if ((dates[i] - dates[i - 1]).Days == 1)
                {
                    consecutive++;
                    if (consecutive >= requiredConsecutive)
                        return true;
                }
                else if ((dates[i] - dates[i - 1]).Days > 1)
                {
                    consecutive = 1;
                }
            }
            return false;
        }
        

        public async Task<ApiResponse> ExerciseFeedBack(string phoneNumber, ExerciseFeedbackDto feedbackDto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found! ", Action = false };

            ExerciseFeedback feedback = feedbackDto.ToExerciseFeedback();
            feedback.AthleteId = athlete.Id;
            feedback.Athlete = athlete;

            feedback.SingleExercise = await context.SingleExercises.FirstOrDefaultAsync(x => x.Id == feedbackDto.SingleExerciseId);
            feedback.TrainingSession = await context.TrainingSessions.FirstOrDefaultAsync(x => x.Id == feedbackDto.TrainingSessionId);
            feedback.Coach = await context.Coaches.FirstOrDefaultAsync(x => x.Id == feedbackDto.CoachId);

            await context.ExerciseFeedbacks.AddAsync(feedback);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "feedback saved.",
                Result = feedback
            };
        }


        public async Task<ApiResponse> ChangeExercise(string phoneNumber, ExerciseChangeDto dto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };

            ExerciseChangeRequest exercisereq = dto.ToExerciseChangeRequest();
            exercisereq.AthleteId = athlete.Id;
            exercisereq.Athlete = athlete;
            exercisereq.SingleExercise = await context.SingleExercises.FirstOrDefaultAsync(x => x.Id == dto.SingleExerciseId);
            exercisereq.TrainingSession = await context.TrainingSessions.FirstOrDefaultAsync(x => x.Id == dto.TrainingSessionId);
            exercisereq.Coach = await context.Coaches.FirstOrDefaultAsync(x => x.Id == dto.CoachId);

            await context.ExerciseChangeRequests.AddAsync(exercisereq);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Exercise change request saved.",
                Result = exercisereq
            };

        }


    }
}
