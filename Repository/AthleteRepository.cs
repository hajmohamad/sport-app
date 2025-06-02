
using System;
using System.Globalization;
using System.Text;
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

using Newtonsoft.Json;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Dtos.ZarinPal.Verify;


namespace sport_app_backend.Repository

{
    public class AthleteRepository(ApplicationDbContext context) : IAthleteRepository
    {
        private static readonly HttpClient Client = new HttpClient();

        private async Task<ApiResponse> ConfirmTransactionId(Payment payment)
        {
            try
            {
                payment.CoachService.NumberOfSell += 1;

                var workoutProgram = new WorkoutProgram
                {
                    Title = payment.CoachService.Title,
                    Coach = payment.Coach,
                    CoachId = payment.Coach.Id,
                    Athlete = payment.Athlete,
                    AthleteId = payment.Athlete.Id,
                    Payment = payment,
                    PaymentId = payment.Id
                };

                payment.WorkoutProgram = workoutProgram;
                payment.PaymentStatus = PaymentStatus.SUCCESS;

                await context.WorkoutPrograms.AddAsync(workoutProgram);
                await context.SaveChangesAsync();

                return new ApiResponse
                {
                    Message = "Payment confirmed successfully",
                    Action = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Message = $"Error confirming transaction: {ex.Message}",
                    Action = false
                };
            }
        }

        public async Task<ApiResponse> VerifyPaymentAsync(ZarinPalVerifyRequestDto request)
        {
            var payment = await context.Payments
                .Include(p => p.Coach)
                .Include(p => p.CoachService)
                .Include(p => p.Athlete)
                .Include(p => p.WorkoutProgram)
                .FirstOrDefaultAsync(x => x.Authority == request.Authority);

            if (payment == null)
            {
                return new ApiResponse
                {
                    Message = "Payment not found",
                    Action = false
                };
            }

            var data = new
            {
                merchant_id = request.MerchantId,
                authority = request.Authority,
                amount = payment.Amount,
                currency = "IRT"

            };

            var jsonData = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                var response =
                    await Client.PostAsync("https://sandbox.zarinpal.com/pg/v4/payment/verify.json", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseContent);

                if (result?.data != null && result.data.code == 100)
                {
                    var confirmResult = await ConfirmTransactionId(payment);
                    if (!confirmResult.Action)
                    {
                        return confirmResult;
                    }

                    return new ApiResponse
                    {
                        Action = true,
                        Message = result.data.ref_id
                    };
                }
                else
                {
                    string error = result?.errors?.message ?? "Unknown error from payment gateway.";
                    return new ApiResponse
                    {
                        Action = false,
                        Message = error
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = $"Error verifying payment: {ex.Message}"
                };
            }
        }

        private static async Task<ZarinPalPaymentResponseDto> _requestPaymentAsync(ZarinPalPaymentRequestDto request)
        {
            var data = new
            {
                merchant_id = request.merchant_id,
                amount = request.amount,
                callback_url = request.callback_url,
                description = request.description,
                currency = "IRT"
            };

            var jsonData = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                var response =
                    await Client.PostAsync("https://sandbox.zarinpal.com/pg/v4/payment/request.json", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseContent);

                if (result?.data != null && result.data.code == 100)
                {
                    var authority = result.data.authority;
                    var paymentUrl = $"https://sandbox.zarinpal.com/pg/StartPay/{authority}";

                    return new ZarinPalPaymentResponseDto
                    {
                        PaymentUrl = paymentUrl,
                        Authority = authority,
                        IsSuccessful = true
                    };
                }

                return new ZarinPalPaymentResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = result?.errors?.message ?? "Unknown error"
                };
            }
            catch (Exception ex)
            {
                return new ZarinPalPaymentResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Error sending payment request: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse> BuyCoachingService(string phoneNumber, int coachingServiceId)
        {
            var athlete = await context.Athletes
                .Include(x => x.AthleteQuestions)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

            if (athlete == null)
                return new ApiResponse { Message = "User is not an athlete", Action = false };

            var lastQuestion = athlete.AthleteQuestions.LastOrDefault();
            if (lastQuestion == null)
                return new ApiResponse { Message = "User has not completed the questions", Action = false };

            var coachService = await context.CoachServices
                .Include(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == coachingServiceId && x.IsActive);

            if (coachService == null)
                return new ApiResponse { Message = "CoachingService not found", Action = false };

            if (coachService.IsDeleted)
                return new ApiResponse { Message = "CoachingService is deleted", Action = false };

            var zarinPalResponse = await _requestPaymentAsync(new ZarinPalPaymentRequestDto
            {
                amount = (long)coachService.Price,
                callback_url = "https://charset7.liara.run/api/Athlete/VerifyPayment",
                description = "خرید",
                Mobile = athlete.PhoneNumber
            });

            if (!zarinPalResponse.IsSuccessful)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = zarinPalResponse.ErrorMessage
                };
            }

            var payment = new Payment
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id,
                CoachId = coachService.CoachId,
                Coach = coachService.Coach,
                Authority = zarinPalResponse.Authority,
                Amount = coachService.Price,
                AthleteQuestion = lastQuestion
            };

            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "get url successfully",
                Result = zarinPalResponse
            };
        }

        public async Task<ApiResponse> GetMonthlyActivity(string phoneNumber, int year, int month)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(month);
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var persianCalendar = new System.Globalization.PersianCalendar();

            try
            {
                var startDate = persianCalendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
                var endDate = month == 12
                    ? persianCalendar.ToDateTime(year + 1, 1, 1, 0, 0, 0, 0)
                    : persianCalendar.ToDateTime(year, month + 1, 1, 0, 0, 0, 0);

                var activities = await context.Activities
                    .Where(x => x.AthleteId == athlete.Id && x.Date >= startDate && x.Date < endDate)
                    .ToListAsync();

                return new ApiResponse()
                {
                    Message = "Activities found",
                    Action = true,
                    Result = activities.Select(x => new ActivityDto()
                    {
                        Id = x.Id,
                        Date = x.Date.ToString("yyyy-MM-dd"),
                        CaloriesLost = x.CaloriesLost,
                        Duration   = x.Duration,
                        ActivityCategory = x.ActivityCategory.ToString(),
                        Name= x.Name ?? ""
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse()
                {
                    Message = $"Error converting date: {ex.Message}",
                    Action = false
                };
            }
        }

        public async Task<ApiResponse> GetLastWeekActivity(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var today = DateTime.Now.Date;
            var daysSinceSaturday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 6;
            var lastSaturday = today.AddDays(daysSinceSaturday);

            var activities = await context.Activities
                .Where(x => x.AthleteId == athlete.Id && x.Date >= lastSaturday)
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Activities found",
                Action = true,
                Result = activities.Select(x => new ActivityDto()
                {
                    Id = x.Id,
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    CaloriesLost = x.CaloriesLost,
                    Duration   = x.Duration,
                    ActivityCategory = x.ActivityCategory.ToString(),
                    Name= x.Name ?? ""
                }).ToList()
            };
        }

        public async Task<ApiResponse> TodayActivityReport(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var today = DateTime.Now.Date;
            var activities = await context.Activities
                .Where(x => x.AthleteId == athlete.Id && x.Date == today)
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Activities found",
                Action = true,
                Result = activities.Select(x => new ActivityDto()
                {
                    Id = x.Id,
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    CaloriesLost = x.CaloriesLost,
                    Duration   = x.Duration,
                    ActivityCategory = x.ActivityCategory.ToString(),
                    Name= x.Name ?? ""
                }).ToList()
            };
        }

        public async Task<ApiResponse> AddActivity(string phoneNumber, AddActivityDto addSportDto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete

            var activityCategory = Enum.Parse<ActivityCategory>(addSportDto.ActivityCategory!);

            var sport = new Activity()
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                ActivityCategory = activityCategory,
                CaloriesLost = addSportDto.CaloriesLost,
                Distance = addSportDto.Distance,
                Duration = addSportDto.Duration,
                Date = Convert.ToDateTime(addSportDto.Date)
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
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete
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

        public async Task<ApiResponse> SearchCoaches(CoachNameSearchDto coachNameSearchDto)
        {
            var coaches = await context.Users.Where(c =>
                (c.FirstName + " " + c.LastName).Contains(coachNameSearchDto.FullName) &&
                c.TypeOfUser == TypeOfUser.COACH).ToListAsync();
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
                .Where(q => q.AthleteId == athlete.Id).Include(i => i.InjuryArea)
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

        public async Task<ApiResponse> SubmitAthleteQuestions(string phoneNumber, AthleteQuestionDto athleteQuestionDto)
        {
            var athlete = await context.Athletes.Include(x => x.User)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete
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

        public async Task<ApiResponse> AthleteFirstQuestions(string phoneNumber,
            AthleteFirstQuestionsDto athleteFirstQuestionsDto)
        {
            var user = await context.Users.Include(a => a.Athlete)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
            var athlete = user.Athlete;
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete
            athlete.Height = athleteFirstQuestionsDto.Height;
            athlete.CurrentWeight = athleteFirstQuestionsDto.CurrentWeight;
            user.LastName = athleteFirstQuestionsDto.LastName;
            user.FirstName = athleteFirstQuestionsDto.FirstName;
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

        public async Task<ApiResponse> UpdateWaterInDay(string phoneNumber, int numberOfCup)
        {
            var athlete = context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
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

            if (numberOfCup > 0)
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
            return new ApiResponse() { Message = "WaterInDay is zero", Action = false };

        }
          
        

        public async Task<ApiResponse> UpdateGoalWeight(string phoneNumber, double goalWeight)
        {
            var athlete = context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
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

            var athlete = context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete
            athlete.CurrentWeight = weight;
            var weightEntry = context.WeightEntries.FirstOrDefault(x =>
                x.AthleteId == athlete.Id && x.CurrentDate.Date == DateTime.Now.Date);
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


        public async Task<ApiResponse> UpdateHightWeight(string phoneNumber, double weight, int hight)
        {
            var athlete = context.Athletes.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete
            athlete.CurrentWeight = weight;
            athlete.Height = hight;
            var weightEntry = context.WeightEntries.FirstOrDefault(x =>
                x.AthleteId == athlete.Id && x.CurrentDate.Date == DateTime.Now.Date);
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
                Message = "Weight and hight updated successfully",
                Action = true
            };
        }


        public async Task<ApiResponse> GetLastMonthWeightReport(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };


            var pc = new PersianCalendar();
            var today = DateTime.Now;
            var persianYear = pc.GetYear(today);
            var persianMonth = pc.GetMonth(today);
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
                .ThenInclude(p => p.Payment)
                .ThenInclude(s => s.CoachService)
                .ThenInclude(x => x.Coach)
                .ThenInclude(u => u.User)
                .FirstOrDefaultAsync(z => z.PhoneNumber == phoneNumber);
            if (athlete is null)
                return new ApiResponse()
                    { Action = false, Message = "Athlete not found" };


            return new ApiResponse()
            {
                Action = true,
                Message = "Payments found",
                Result = athlete.WorkoutPrograms.Select(x => x.ToAllWorkoutProgramResponseDto()).ToList()
            };

        }

        public async Task<ApiResponse> GetPayment(string phoneNumber, int paymentId)
        {
            var payment = await context.Payments
                .Include(p => p.Athlete)
                .ThenInclude(a => a.User)
                .Include(x => x.Coach) // بارگذاری Coac
                .ThenInclude(a => a.User)
                .Include(a => a.AthleteQuestion) // بارگذاری User داخل Athlete
                .ThenInclude(I => I!.InjuryArea)
                .Include(w => w.WorkoutProgram)
                .ThenInclude(z => z.ProgramInDays)
                .ThenInclude(z => z.AllExerciseInDays)
                .ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync(p => p.Athlete.PhoneNumber == phoneNumber && p.Id == paymentId);
            if (payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
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
            if (athlete is null)
                return new ApiResponse()
                    { Action = false, Message = "Athlete not found" };
            var workoutProgram = athlete.WorkoutPrograms.Find(w => w.Id == programId);
            if (workoutProgram is null)
            {
                return new ApiResponse()
                    { Action = false, Message = "workout program not found" };

            }

            if (
                workoutProgram.Status == WorkoutProgramStatus.ACTIVE)
            {
                var allTrainingSessions =
                    await context.TrainingSessions
                        .Where(t => t.WorkoutProgramId == programId)
                        .ToListAsync();

                foreach (var trainingSession in allTrainingSessions)
                {
                    trainingSession.TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED;
                    var arrayBitMap = trainingSession.ExerciseCompletionBitmap.ToArray();
                    Array.Clear(arrayBitMap, 0, arrayBitMap.Length);
                    trainingSession.ExerciseCompletionBitmap = arrayBitMap;
                }

                await context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = true,
                    Message = "Program clear",
                };

            }

            foreach (var program in athlete.WorkoutPrograms.Where(x => x.Status == WorkoutProgramStatus.ACTIVE))
            {
                program.Status = WorkoutProgramStatus.STOPPED;
            }




            if (workoutProgram.Status is WorkoutProgramStatus.WRITING or WorkoutProgramStatus.NOTSTARTED)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "workout program status is not accept"
                };
            }

            workoutProgram.Status = WorkoutProgramStatus.ACTIVE;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "Program found",
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
            });
        }



        public async Task<ApiResponse> ExerciseFeedBack(string phoneNumber, ExerciseFeedbackDto feedbackDto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found! ", Action = false };
            var singleExercise = await context.SingleExercises.Include(p => p.ProgramInDay)
                .ThenInclude(w => w!.WorkoutProgram)
                .FirstOrDefaultAsync(s => s.Id == feedbackDto.SingleExerciseId);

            var feedback = feedbackDto.ToExerciseFeedback();
            feedback.AthleteId = athlete.Id;
            if (singleExercise?.ProgramInDay?.WorkoutProgram != null)
                feedback.CoachId = singleExercise.ProgramInDay.WorkoutProgram.CoachId;
            feedback.SingleExercise =
                await context.SingleExercises.FirstOrDefaultAsync(x => x.Id == feedbackDto.SingleExerciseId);
            feedback.TrainingSession =
                await context.TrainingSessions.FirstOrDefaultAsync(x => x.Id == feedbackDto.TrainingSessionId);

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
            var singleExercise = await context.SingleExercises.Include(p => p.ProgramInDay)
                .ThenInclude(w => w.WorkoutProgram)
                .FirstOrDefaultAsync(s => s.Id == dto.SingleExerciseId);
            var exerciseChangeRequest = dto.ToExerciseChangeRequest();
            exerciseChangeRequest.AthleteId = athlete.Id;
            exerciseChangeRequest.SingleExerciseId = dto.SingleExerciseId;
            exerciseChangeRequest.TrainingSessionId = dto.TrainingSessionId;
            if (singleExercise?.ProgramInDay?.WorkoutProgram != null)
                exerciseChangeRequest.CoachId = singleExercise.ProgramInDay.WorkoutProgram.CoachId;

            await context.ExerciseChangeRequests.AddAsync(exerciseChangeRequest);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Exercise change request saved.",
                Result = exerciseChangeRequest
            };

        }


        public async Task<ApiResponse> GetAllTrainingSession(string phoneNumber)
        {
            var athlete = await context.Athletes.Include(a => a.WorkoutPrograms)
                .FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };
            var workoutProgram = athlete.WorkoutPrograms.FirstOrDefault(x =>
                x.Status == WorkoutProgramStatus.ACTIVE);
            if (workoutProgram is null)
                return new ApiResponse() { Message = "workoutProgram not found", Action = true };

            var trainingSessions =
                await context.TrainingSessions.Include(T => T.ProgramInDay)
                    .ThenInclude(p => p.AllExerciseInDays)
                    .ThenInclude(z => z.Exercise)
                    .Where(t => t.WorkoutProgram.Id == workoutProgram.Id).ToListAsync();

            return new ApiResponse()
            {
                Action = true,
                Message = "get TrainingSession",
                Result = trainingSessions.Select(y => y.ToAllTrainingSessionDto())
            };



        }

        public async Task<ApiResponse> GetTrainingSession(string phoneNumber, int trainingSessionId)
        {
            var trainingSession = await context.TrainingSessions.Include(p => p.ProgramInDay)
                .ThenInclude(a => a.AllExerciseInDays).ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };



            return new ApiResponse()
            {
                Action = true,
                Message = "get TrainingSession",
                Result = trainingSession.ToTrainingSessionDto()
            };


        }

        public async Task<ApiResponse> DoTrainingSession(string phoneNumber, int trainingSessionId, int exerciseNumber)
        {
            var trainingSession = await context.TrainingSessions
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            trainingSession.TrainingSessionStatus = TrainingSessionStatus.INPROGRESS;

            var bitmap = trainingSession.ExerciseCompletionBitmap.ToArray();
            bitmap[exerciseNumber] = 0xFF;
            trainingSession.ExerciseCompletionBitmap = bitmap;
            var allCompleted = trainingSession.ExerciseCompletionBitmap.All(b => b == 0xFF);
            trainingSession.TrainingSessionStatus =
                allCompleted ? TrainingSessionStatus.COMPLETED : TrainingSessionStatus.INPROGRESS;

            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Action = true,
                Message = "Do Training session",
                Result = trainingSession.ToAllTrainingSessionDto()
            };
        }

        public async Task<ApiResponse> FinishTrainingSession(string phoneNumber,
            FinishTrainingSessionDto finishTrainingSessionDto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };
            var trainingSession = await context.TrainingSessions
                .FirstOrDefaultAsync(z => z.Id == finishTrainingSessionDto.TrainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };
            trainingSession.TrainingSessionStatus = TrainingSessionStatus.COMPLETED;
            var activity = new Activity()
            {
                Athlete = athlete,
                Duration = finishTrainingSessionDto.Duration,
                CaloriesLost = finishTrainingSessionDto.CaloriesLost,
                ActivityCategory = ActivityCategory.EXERCISE,
                Name = finishTrainingSessionDto.TrainingSessionName
            };

            await context.Activities.AddAsync(activity);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "Finish Training session",
                Result = activity
            };


        }


        public async Task<ApiResponse> FeedbackTrainingSession(string phoneNumber,
            FeedbackTrainingSessionDto feedbackTrainingSessionDto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };
            var trainingSession = await context.TrainingSessions
                .FirstOrDefaultAsync(z => z.Id == feedbackTrainingSessionDto.TrainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };
            if (trainingSession.TrainingSessionStatus != TrainingSessionStatus.COMPLETED)
                return new ApiResponse() { Message = "trainingSession not completed", Action = false };
            trainingSession.ExerciseFeeling =
                Enum.Parse<ExerciseFeeling>(feedbackTrainingSessionDto.ExerciseFeeling ?? string.Empty);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "Feedback Training session",
                Result = trainingSession
            };
        }


        public async Task<ApiResponse> ResetTrainingSession(string phoneNumber, int trainingSessionId)
        {
            var trainingSession = await context.TrainingSessions
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            trainingSession.TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED;

            var arrayBitMap = trainingSession.ExerciseCompletionBitmap.ToArray();
            Array.Clear(arrayBitMap, 0, arrayBitMap.Length);
            trainingSession.ExerciseCompletionBitmap = arrayBitMap;
            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Action = true,
                Message = "Training session Retested",
                Result = trainingSession.ToAllTrainingSessionDto()
            };
        }

        public async Task<ApiResponse> GetActivityPage(string phoneNumber){
            var athlete = await context.Athletes
                .Include(a => a.WeightEntries)
                .Include(a => a.WaterInDays)
                .Include(a => a.Activities).Include(athlete => athlete.WaterInTake)
                .FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);

            if (athlete is null)
                return new ApiResponse { Message = "Athlete not found", Action = false };
            var waterInTake = athlete.WaterInTake ?? new WaterInTake()
            {
                DailyCupOfWater = 0,
                Reminder = 0
            };

            var today = DateTime.Today.Date;
            var lastSaturday = GetLastSaturday(today);
            var firstDayOfPersianMonth = GetFirstDayOfPersianMonth(today);

            var allActivities = athlete.Activities.ToList();

            var totalActivities = allActivities.Count;
            var totalTime = allActivities.Select(a => a.Duration).DefaultIfEmpty(0).Sum();
            var totalCalories = allActivities.Select(a => a.CaloriesLost).DefaultIfEmpty(0).Sum();

            var lastWeekActivities = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = lastSaturday.AddDays(offset).Date;
                    return athlete.Activities.Any(a => a.Date.Date == date) ? 1 : 0;
                })
                .ToList();

            var todayWater = athlete.WaterInDays.FirstOrDefault(w => w.Date == today);
              

            var todayActivities = athlete.Activities
                .Where(a => a.Date.Date == today)
                .Select(a => new
                {
                    a.Id,
                    Date = a.Date.ToString("yyyy-MM-dd"),
                    a.CaloriesLost,
                    a.Duration,
                    ActivityCategory = a.ActivityCategory.ToString(),
                    a.Name
                })
                .ToList();

            var currentWeight = athlete.CurrentWeight;
            var goalWeight = athlete.WeightGoal;

            var lastMonthWeights = athlete.WeightEntries
                .Where(w => w.CurrentDate >= firstDayOfPersianMonth)
                .OrderByDescending(w => w.CurrentDate)
                .Select(w => new
                {
                    Date = w.CurrentDate.ToString("yyyy-MM-dd"),
                    w.Weight
                })
                .ToList();

            return new ApiResponse
            {
                Message = "Activities found",
                Action = true,
                Result = new
                {
                    totalActivities,
                    totalTime,
                    totalCalories,
                    lastWeekActivities ,
                    NumberOfCupsDrinked=   todayWater?.NumberOfCupsDrinked ?? 0,
                    waterInTake.DailyCupOfWater,
                    waterInTake.Reminder,
                    activityThisDay = todayActivities,
                    currentWeight,
                    goalWeight,
                    lastMonthWeight = lastMonthWeights,
                    date= DateTime.Now
                }
            };
        }

        public  DateTime GetLastSaturday(DateTime today)
        {
            int diff = (7 + (int)today.DayOfWeek - 6) % 7;
            return today.AddDays(-diff);
        }


        public  DateTime GetFirstDayOfPersianMonth(DateTime date)
        {
            var pc = new PersianCalendar();
            var year = pc.GetYear(date);
            var month = pc.GetMonth(date);
            return pc.ToDateTime(year, month, 1, 0, 0, 0, 0);
        }


    }
}
