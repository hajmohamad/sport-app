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
using sport_app_backend.Models.TrainingPlan;


namespace sport_app_backend.Repository

{
    public class AthleteRepository(ApplicationDbContext context, IZarinPal zarinPal) : IAthleteRepository
    {
        public async Task<ApiResponse> GetFaq()
        {
            var getFaq = await context.AthleteFaq.AsNoTracking().ToListAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "get CoachFaq",
                Result = getFaq
            };
        }

        private async Task<ApiResponse> ConfirmTransactionId(Payment payment, long refId)
        {
            try
            {
                payment.CoachService.NumberOfSell += 1;
                payment.RefId = refId;

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
                payment.Coach.Amount += (payment.Amount - payment.Coach.ServiceFee);
                payment.AppFee = payment.Coach.ServiceFee;

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
                    Action = false,
                    Message = "تراکنشی یافت نشد",
                };
            }

            request.Amount = payment.Amount;

            try
            {
                var result = await zarinPal.VerifyPaymentAsync(request);
                switch (result?.Data)
                {
                    case { Code: 100 }:
                    {
                        var confirmResult = await ConfirmTransactionId(payment, result.Data.Ref_id);
                        if (!confirmResult.Action)
                        {
                            return confirmResult;
                        }

                        return new ApiResponse
                        {
                            Action = true,
                            Message = "پرداخت با موفقیت انجام شد ",
                            Result = result.Data.Ref_id
                        };
                    }
                    case { Code: 101 }:
                        return new ApiResponse
                        {
                            Action = true,
                            Message = "پرداخت با موفقیت انجام شد و قبلا تایید شده است  ",
                            Result = result.Data.Ref_id
                        };
                }

                if (result?.Errors is null)
                    return new ApiResponse
                    {
                        Action = false,
                        Message = "خطا ناشناخته",
                    };
                payment.PaymentStatus = PaymentStatus.FAILED;
                await context.SaveChangesAsync();

                var error = result?.Errors?.Select(e => e.Message).ToString() ??
                            "Unknown error from payment gateway.";
                return new ApiResponse
                {
                    Action = false,
                    Message = "پرداخت ناموفق",
                    Result = error
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying payment: {ex.Message}");
                return new ApiResponse
                {
                    Action = false,
                    Message = $"Error verifying payment: {ex.Message}"
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

            if (coachService.IsDeleted || !coachService.IsActive)
                return new ApiResponse { Message = "CoachingService is deleted or not active", Action = false };

            var zarinPalResponse = await zarinPal.RequestPaymentAsync(new ZarinPalPaymentRequestDto
            {
                amount = (long)coachService.Price,
                callback_url = "https://chaarset.ir/payment/verify",
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
            var athleteId = await context.Athletes
                .AsNoTracking()
                .Where(x => x.PhoneNumber == phoneNumber)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (athleteId is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }

            if (!Enum.TryParse<ActivityCategory>(addSportDto.ActivityCategory, true, out var activityCategory))
            {
                return new ApiResponse() { Message = "Invalid activity category provided", Action = false };
            }

            var sport = new Activity()
            {
                AthleteId = athleteId.Value,
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

        public async Task<ApiResponse> SearchCoaches(CoachNameSearchDto coachNameSearchDto)
        {
            if (string.IsNullOrWhiteSpace(coachNameSearchDto.FullName))
            {
                return new ApiResponse()
                {
                    Message = "No search term provided",
                    Action = true,
                    Result = new List<CoachForSearch>() // Return an empty list
                };
            }

            // --- The Main Optimization ---
            var coaches = await context.Users
                .AsNoTracking()
                .Where(c => c.TypeOfUser == TypeOfUser.COACH &&
                            (c.FirstName.Contains(coachNameSearchDto.FullName) ||
                             c.LastName.Contains(coachNameSearchDto.FullName) ||
                             (c.FirstName + " " + c.LastName).Contains(coachNameSearchDto.FullName)))
                .Select(c => c.ToCoachForSearch())
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Coaches found",
                Action = true,
                Result = coaches
            };
        }

        public async Task<ApiResponse> GetLastQuestion(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }

            var lastQuestion = await context.AthleteQuestions
                .Where(q => q.AthleteId == athlete.Id).Include(i => i.InjuryArea)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync();
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
            var weightEntry = new WeightEntry()
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                CurrentDate = DateTime.Now,
                Weight = athleteFirstQuestionsDto.CurrentWeight
            };
            await context.WeightEntries.AddAsync(weightEntry);
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

        public async Task<ApiResponse> UpdateHightWeight(string phoneNumber, double weight, int hight)
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
            var paymentDtos = await context.WorkoutPrograms
                .AsNoTracking()
                .Where(wp => wp.Athlete.PhoneNumber == phoneNumber)
                .OrderByDescending(wp => wp.Payment.PaymentDate)
                .Select(wp => new AllPaymentResponseDto
                {
                    PaymentId = wp.PaymentId,
                    PaymentStatus = wp.Payment.PaymentStatus.ToString(),
                    Name = wp.Coach.User.FirstName + " " + wp.Coach.User.LastName,
                    Amount = wp.Payment.Amount.ToString(),
                    DateTime = wp.Payment.PaymentDate.ToString("yyyy-MM-dd"),
                    ImageProfile = wp.Coach.User.ImageProfile,
                    CoachServiceTitle = wp.Payment.CoachService.Title,
                    WorkoutProgramStatus = wp.Status.ToString()
                })
                .ToListAsync();

            if (!paymentDtos.Any())
            {
                return new ApiResponse()
                {
                    Action = true,
                    Message = "No payment history found",
                    Result = new List<AllPaymentResponseDto>()
                };
            }

            return new ApiResponse()
            {
                Action = true,
                Message = "Payments found",
                Result = paymentDtos
            };
        }

        public async Task<ApiResponse> GetPayment(string phoneNumber, int paymentId)
        {
            var payment = await context.Payments
                .AsNoTracking()
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

            if (athlete == null)
            {
                return new ApiResponse { Action = false, Message = "Athlete not found" };
            }

            var targetProgram = athlete.WorkoutPrograms.FirstOrDefault(w => w.Id == programId);

            if (targetProgram == null)
            {
                return new ApiResponse { Action = false, Message = "Workout program not found" };
            }

            var allTrainingSessions = await context.TrainingSessions
                .Where(t => t.WorkoutProgramId == programId)
                .ToListAsync();

            Action<TrainingSession> resetTrainingSession = ts =>
            {
                ts.TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED;
                var arrayBitMap = ts.ExerciseCompletionBitmap.ToArray();
                Array.Clear(arrayBitMap, 0, arrayBitMap.Length);
                ts.ExerciseCompletionBitmap = arrayBitMap;
            };

            if (targetProgram.Status == WorkoutProgramStatus.ACTIVE)
            {
                athlete.ActiveWorkoutProgramId = programId;
                allTrainingSessions.ForEach(resetTrainingSession);
                await context.SaveChangesAsync();
                return new ApiResponse { Action = true, Message = "Program already active and reset." };
            }

            foreach (var program in athlete.WorkoutPrograms.Where(x => x.Status == WorkoutProgramStatus.ACTIVE))
            {
                program.Status = WorkoutProgramStatus.STOPPED;
            }

            switch (targetProgram.Status)
            {
                case WorkoutProgramStatus.WRITING:
                case WorkoutProgramStatus.NOTSTARTED:
                    return new ApiResponse
                        { Action = false, Message = "Workout program status is not acceptable for activation." };

                case WorkoutProgramStatus.STOPPED:
                case WorkoutProgramStatus.FINISHED:
                    allTrainingSessions.ForEach(resetTrainingSession);
                    break;

                case WorkoutProgramStatus.NOTACTIVE:
                    await AddTrainingSession(targetProgram.PaymentId);
                    break;
            }

            targetProgram.Status = WorkoutProgramStatus.ACTIVE;
            athlete.ActiveWorkoutProgramId = programId;

            await context.SaveChangesAsync();
            return new ApiResponse { Action = true, Message = "Program activated successfully." };
        }

        private async Task AddTrainingSession(int paymentId)
        {
            var workoutProgram = await context.WorkoutPrograms
                .Include(p => p.Payment)
                .ThenInclude(p => p.AthleteQuestion)
                .Include(p => p.ProgramInDays)
                .ThenInclude(d => d.AllExerciseInDays)
                .FirstAsync(p => p.PaymentId == paymentId);

            var numberOfDay = workoutProgram.ProgramDuration *
                              workoutProgram.Payment.AthleteQuestion.DaysPerWeekToExercise;
            var programInDayList = workoutProgram.ProgramInDays;
            var programInDayCount = programInDayList.Count;

            var sessions = new List<TrainingSession>();

            for (var day = 1; day <= numberOfDay; day++)
            {
                var index = day % programInDayCount;
                sessions.Add(new TrainingSession
                {
                    ProgramInDayId = programInDayList[index].Id,
                    ProgramInDay = programInDayList[index],
                    ExerciseCompletionBitmap = new byte[programInDayList[index].AllExerciseInDays.Count],
                    TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED,
                    DayNumber = day,
                    WorkoutProgram = workoutProgram,
                    WorkoutProgramId = workoutProgram.Id
                });
            }

            await context.TrainingSessions.AddRangeAsync(sessions);
            await context.SaveChangesAsync();
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
            var athlete = await context.Athletes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

            if (athlete is null)
            {
                return new ApiResponse() { Message = "ورزشکار یافت نشد!", Action = false };
            }


            var trainingSession = await context.TrainingSessions
                .AsNoTracking()
                .Include(ts => ts.WorkoutProgram)
                .Include(ts => ts.ProgramInDay)
                .ThenInclude(pid => pid.AllExerciseInDays)
                .FirstOrDefaultAsync(ts => ts.Id == feedbackDto.TrainingSessionId);


            if (trainingSession is null)
            {
                return new ApiResponse() { Message = "جلسه تمرینی یافت نشد!", Action = false };
            }

            if (trainingSession.WorkoutProgram?.AthleteId != athlete.Id)
            {
                return new ApiResponse() { Message = "شما به این جلسه تمرینی دسترسی ندارید.", Action = false };
            }

            if (trainingSession.ProgramInDay.AllExerciseInDays.All(se => se.Id != feedbackDto.SingleExerciseId))
            {
                return new ApiResponse() { Message = "تمرین مشخص شده در این جلسه وجود ندارد.", Action = false };
            }


            var feedback = feedbackDto.ToExerciseFeedback();
            feedback.AthleteId = athlete.Id;
            feedback.CoachId = trainingSession.WorkoutProgram.CoachId;

            await context.ExerciseFeedbacks.AddAsync(feedback);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "فیدبک شما با موفقیت ثبت شد.",
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
            };
        }


        public async Task<ApiResponse> GetAllTrainingSession(string phoneNumber)
        {
            var resultData = await context.WorkoutPrograms
                .AsNoTracking()
                .Where(wp => wp.Athlete.PhoneNumber == phoneNumber && wp.Status == WorkoutProgramStatus.ACTIVE)
                .Select(wp => new
                {
                    ProgramName = wp.Title,
                    TrainingSessions = wp.TrainingSessions.Select(ts => new AllTrainingSessionDto
                    {
                        Id = ts.Id,
                        DayNumber = ts.DayNumber,
                        TrainingSessionStatus = ts.TrainingSessionStatus.ToString(),
                        ExerciseCompletionBitmap = ts.ExerciseCompletionBitmap.GetExerciseStatusArray()
                    }).ToList()
                })
                .FirstOrDefaultAsync(); // Get the single active program's data.

            if (resultData == null)
            {
                return new ApiResponse() { Message = "Active workout program not found", Action = true, Result = null };
            }

            return new ApiResponse()
            {
                Action = true,
                Message = "Training sessions retrieved successfully",
                Result = new
                {
                    ToAllTrainingSession = resultData.TrainingSessions,
                    ProgramName = resultData.ProgramName
                }
            };
        }

        public async Task<ApiResponse> GetTrainingSession(string phoneNumber, int trainingSessionId)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };

            var trainingSession = await context.TrainingSessions
                .Include(ts => ts.WorkoutProgram)
                .Include(p => p.ProgramInDay)
                .ThenInclude(a => a.AllExerciseInDays).ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);

            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            var finalCalories = _CalculateCaloriesInternal(trainingSession, athlete.CurrentWeight, false);


            return new ApiResponse()
            {
                Action = true,
                Message = "get TrainingSession",
                Result = trainingSession.ToTrainingSessionDto(finalCalories)
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
            try
            {
                var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
                if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };

                var trainingSession = await context.TrainingSessions
                    .Include(ts => ts.WorkoutProgram)
                    .Include(ts => ts.ProgramInDay)
                    .ThenInclude(pid => pid.AllExerciseInDays)
                    .ThenInclude(se => se.Exercise) // اطمینان از بارگذاری اطلاعات هر حرکت
                    .FirstOrDefaultAsync(z => z.Id == finishTrainingSessionDto.TrainingSessionId);

                if (trainingSession is null)
                    return new ApiResponse() { Message = "trainingSession not found", Action = false };
                var athleteWeight = athlete.CurrentWeight;

                var finalCalories = _CalculateCaloriesInternal(trainingSession, athleteWeight, true);


                trainingSession.TrainingSessionStatus = TrainingSessionStatus.COMPLETED;
                trainingSession.WorkoutProgram.LastExerciseDate = DateTime.Now.Date;
                trainingSession.WorkoutProgram.CompletedSessionCount++;

                var activity = new Activity()
                {
                    AthleteId = athlete.Id,
                    Duration = finishTrainingSessionDto.Duration,
                    CaloriesLost = finalCalories,
                    ActivityCategory = ActivityCategory.EXERCISE,
                    Name = finishTrainingSessionDto.TrainingSessionName,
                    Date = DateTime.Now.Date
                };

                await context.Activities.AddAsync(activity);
                await context.SaveChangesAsync();

                return new ApiResponse()
                {
                    Action = true,
                    Message = "Finish Training session",
                    Result = activity.ToActivityDto()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = $"Error finishing: {ex.Message}"
                };
            }
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
                Message = "Feedback Training session"
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

        public async Task<ApiResponse> GetActivityPage(string phoneNumber)
        {
            var today = DateTime.Today;
            var lastSaturday = GetLastSaturday(today);
            var firstDayOfPersianMonth = GetFirstDayOfPersianMonth(today);

          
            var activityPageData = await context.Athletes
                .AsNoTracking() //
                .Where(a => a.PhoneNumber == phoneNumber)
                .Select(a => new
                {
                    IsAthleteFound = true,
                    CurrentWeight = a.CurrentWeight,
                    GoalWeight = a.WeightGoal,

                    DailyCupOfWater = a.WaterInTake != null ? a.WaterInTake.DailyCupOfWater : 0,
                    Reminder = a.WaterInTake != null ? a.WaterInTake.Reminder : 0,
                    
                    Name = a.User.FirstName + " "+a.User.LastName,
                    Height= a.Height,

                    TotalActivities = a.Activities.Count(),
                  
                    TotalTime = a.Activities.Sum(act => (double?)act.Duration) ?? 0,
                    TotalCalories = a.Activities.Sum(act => (double?)act.CaloriesLost) ?? 0,

                    TodayActivities = a.Activities
                        .Where(act => act.Date == today)
                        .Select(act => new ActivityDto
                        {
                            Id = act.Id,
                            Date = act.Date.ToString("yyyy-MM-dd"),
                            CaloriesLost = act.CaloriesLost,
                            Duration = act.Duration,
                            ActivityCategory = act.ActivityCategory.ToString(),
                            Name = act.Name ?? ""
                        }).ToList(),

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

                    NumberOfCupsDrinked = a.WaterInDays
                        .Where(w => w.Date == today)
                        .Select(w => w.NumberOfCupsDrinked)
                        .FirstOrDefault() // مقدار ۰ به صورت پیش‌فرض برمی‌گرداند
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

            return new ApiResponse
            {
                Message = "Activities found",
                Action = true,
                Result = new ActivityPageDto()
                {   Name = activityPageData.Name,
                    Height = activityPageData.Height,
                    TotalActivities = activityPageData.TotalActivities,
                    TotalTime = activityPageData.TotalTime,
                    TotalCalories = activityPageData.TotalCalories,
                    LastWeekActivities = lastWeekActivitiesBitmap,
                    NumberOfCupsDrinked = activityPageData.NumberOfCupsDrinked,
                    DailyCupOfWater = activityPageData.DailyCupOfWater,
                    Reminder = activityPageData.Reminder,
                    TodayActivities = activityPageData.TodayActivities,
                    CurrentWeight = activityPageData.CurrentWeight,
                    GoalWeight = activityPageData.GoalWeight,
                    LastMonthWeights = activityPageData.LastMonthWeights,
                }
            };
        }

        public async Task<ApiResponse> CalculateCalories(string phoneNumber, int trainingSessionId)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };


            var trainingSession = await context.TrainingSessions
                .Include(ts => ts.WorkoutProgram)
                .Include(ts => ts.ProgramInDay)
                .ThenInclude(pid => pid.AllExerciseInDays)
                .ThenInclude(se => se.Exercise)
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);

            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            var athleteWeight = athlete.CurrentWeight;

            var finalCalories = _CalculateCaloriesInternal(trainingSession, athleteWeight, true);
            return new ApiResponse()
            {
                Action = true,
                Message = "Calories calculated successfully for completed exercises.",
                Result = finalCalories
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

        private static double _CalculateCaloriesInternal(
            TrainingSession trainingSession, double athleteWeight, bool completedExercisesOnly)
        {
            const double restMet = 1.3;


            // 1. میانگین‌گیری از پارامترها بر اساس اهداف برنامه
            var priorities = trainingSession.WorkoutProgram.ProgramPriorities;
            var avgParams = new TrainingGoalParameter
            {
                RestBetweenSetsSec = priorities.Average(p => TrainingGoalParameters.Parameters[p].RestBetweenSetsSec),
                RestBetweenMovesSec = priorities.Average(p => TrainingGoalParameters.Parameters[p].RestBetweenMovesSec),
                TimePerRepSec = priorities.Average(p => TrainingGoalParameters.Parameters[p].TimePerRepSec),
                EpocPercentage = priorities.Average(p => TrainingGoalParameters.Parameters[p].EpocPercentage)
            };

            double totalCaloriesActiveAndRestSets = 0;
            var exercisesCountInCalculation = 0;
            var allExercises = trainingSession.ProgramInDay.AllExerciseInDays;

            // ۲. محاسبه کالری
            for (var i = 0; i < allExercises.Count; i++)
            {
                // اگر فقط حرکات انجام شده مد نظر است، بیت‌مپ را چک کن
                if (completedExercisesOnly)
                {
                    if (i >= trainingSession.ExerciseCompletionBitmap.Length ||
                        trainingSession.ExerciseCompletionBitmap[i] != 0xFF)
                    {
                        continue; // اگر حرکت انجام نشده، از آن بگذر
                    }
                }

                var exercise = allExercises[i];
                if (exercise.Exercise == null) continue;

                exercisesCountInCalculation++;

                var workTimeSec = exercise.Set * exercise.Rep * avgParams.TimePerRepSec;
                var restBetweenSetsSec = (exercise.Set > 1) ? (exercise.Set - 1) * avgParams.RestBetweenSetsSec : 0;

                // اگر MET صفر بود، یک مقدار پیش‌فرض در نظر می‌گیریم
                var exerciseMet = (exercise.Exercise.Met == 0) ? 3.0 : exercise.Exercise.Met;
                var caloriesActive = (exerciseMet * athleteWeight * workTimeSec) / 3600.0;
                var caloriesRestSets = (restMet * athleteWeight * restBetweenSetsSec) / 3600.0;

                totalCaloriesActiveAndRestSets += caloriesActive + caloriesRestSets;
            }

            if (exercisesCountInCalculation == 0 && completedExercisesOnly)
            {
                return 0.0;
            }


            var totalRestBetweenMovesSec = (exercisesCountInCalculation > 1)
                ? (exercisesCountInCalculation - 1) * avgParams.RestBetweenMovesSec
                : 0;
            var caloriesRestMoves = (restMet * athleteWeight * totalRestBetweenMovesSec) / 3600.0;


            var totalCaloriesBeforeEpoc = totalCaloriesActiveAndRestSets + caloriesRestMoves;


            var finalCalories = totalCaloriesBeforeEpoc * (1 + avgParams.EpocPercentage);

            return Math.Round(finalCalories, 2);
        }
    }
}