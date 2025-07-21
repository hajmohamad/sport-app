using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;


namespace sport_app_backend.Repository
{
    public class CoachRepository(ApplicationDbContext context) : ICoachRepository
    {
        public async Task<ApiResponse> AthleteReportForCoach(int athleteId)
        {
            var athlete = await context.Athletes.Include(u=>u.User)
                .Include(athlete => athlete.Activities)
                .Include(athlete => athlete.WeightEntries).FirstOrDefaultAsync(a => a.Id == athleteId);
              if (athlete is null)
                return new ApiResponse { Message = "Athlete not found", Action = false };
 
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




            var currentWeight = athlete.CurrentWeight;
            var goalWeight = athlete.WeightGoal;

            var lastMonthWeights = athlete.WeightEntries
                .Where(w => w.CurrentDate >= firstDayOfPersianMonth)
                .OrderByDescending(w => w.CurrentDate)
                .Select(w => new WeightReportDto
                {
                    Date = w.CurrentDate.ToString("yyyy-MM-dd"),
                    Weight = w.Weight
                })
                .ToList();

            return new ApiResponse
            {
                Message = "Activities found",
                Action = true,
                Result = new ActivityPageDto()
                {
                    TotalActivities = totalActivities,
                    TotalTime = totalTime,
                    TotalCalories = totalCalories,
                    LastWeekActivities = lastWeekActivities,
                    CurrentWeight = currentWeight,
                    GoalWeight = goalWeight,
                    LastMonthWeights = lastMonthWeights,
                    Height=athlete.Height,
                    Name=athlete.User.FirstName+" "+athlete.User.LastName
                }
            };
        }
        public async Task<ApiResponse> AthleteMonthlyActivityForCoach(int athleteId, int year, int month)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(month);
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.Id == athleteId);
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
                        Duration = x.Duration,
                        ActivityCategory = x.ActivityCategory.ToString(),
                        Name = x.Name ?? ""
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

        public async Task<ApiResponse> AddCoachingServices(string phoneNumber, AddCoachServiceDto addCoachingServiceDto)
        {
            var coach = await context.Coaches.Include(c => c.CoachingServices)
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach is null)
                return new ApiResponse()
                    { Message = "User is not a coach", Action = false }; // Ensure the user is a coach
            if (addCoachingServiceDto.Price < 50000)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "مبلغ وارد شده نمیتواند کمتر از 50 هزار تومان باشد"
                };
            }

            var coachingService = addCoachingServiceDto.ToCoachService(coach);
            coach.CoachingServices ??= [];
            coach.CoachingServices.Add(coachingService);
            context.CoachServices.Add(coachingService);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching Service added successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> SubmitCoachQuestions(string phoneNumber, CoachQuestionDto coachQuestionDto)
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
            var coach = user.Coach;
            if (coach == null)
                return new ApiResponse()
                    { Message = "User is not a coach", Action = false }; // Ensure the user is a coach
            user.FirstName = coachQuestionDto.FirstName;
            user.LastName = coachQuestionDto.LastName;
            var coachQuestion = coachQuestionDto.ToCoachQuestion(user);
            coach.CoachQuestion = coachQuestion;
            await context.CoachQuestions.AddAsync(coachQuestion);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coach questions submitted successfully",
                Action = true,
                Result = new
                {
                    Questions = true
                }
            };
        }

        public async Task<ApiResponse> UpdateCoachingService(string phoneNumber, int id,
            AddCoachServiceDto addCoachingServices)
        {
            var coach = await context.Coaches.Include(x => x.CoachingServices)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null)
                return new ApiResponse()
                    { Message = "User is not a coach", Action = false }; // Ensure the user is a coach
            if (addCoachingServices.Price < 50000)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "مبلغ وارد شده نمیتواند کمتر از 50 هزار تومان باشد"
                };
            }

            var coachingService = coach.CoachingServices
                .FirstOrDefault(x => x.Id == id && !x.IsDeleted);

            if (coachingService is null)
            {
                return new ApiResponse() { Message = "سرویس کوچینگ یافت نشد یا قبلاً حذف شده است.", Action = false };
            }

            var hasActivePayments = await context.Payments
                .AnyAsync(p => p.CoachServiceId == coachingService.Id);

            if (hasActivePayments)
            {
                coachingService.IsDeleted = true;
                var newCoachService = addCoachingServices.ToCoachService(coach);
                newCoachService.NumberOfSell = coachingService.NumberOfSell;
                coach.CoachingServices.Add(newCoachService);
                await context.CoachServices.AddAsync(newCoachService);
            }
            else
            {
                coachingService.UpdateCoachServices(addCoachingServices);
            }

            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching Service updated successfully",
                Action = true,
                Result = coachingService.ToCoachingServiceResponse()
            };
        }

        public async Task<ApiResponse> DeleteCoachingService(string phoneNumber, int id)
        {
            var coach = await context.Coaches.Include(x => x.CoachingServices)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null)
                return new ApiResponse()
                    { Message = "User is not a coach", Action = false }; // Ensure the user is a coach
            var coachingService = coach.CoachingServices.FirstOrDefault(x => x.Id == id);
            if (coachingService is null)
                return new ApiResponse() { Message = "Coaching Service not found", Action = false };
            coachingService.IsDeleted = true;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching Service deleted successfully",
                Action = true,
                Result = coachingService.ToCoachingServiceResponse()
            };
        }

        public async Task<ApiResponse> GetAllPayment(string phoneNumber)
        {
            var payments = await context.Payments
                .Include(p => p.Coach)
                .ThenInclude(c => c!.User)
                .Include(p => p.Athlete)
                .ThenInclude(a => a!.User)
                .Include(p => p.CoachService)
                .Include(p => p.WorkoutProgram)
                .Where(p =>
                    p.Coach != null &&
                    p.Coach.PhoneNumber == phoneNumber &&
                    p.PaymentStatus == PaymentStatus.SUCCESS &&
                    p.WorkoutProgram != null &&
                    (
                        p.WorkoutProgram.Status == WorkoutProgramStatus.NOTSTARTED ||
                        p.WorkoutProgram.Status == WorkoutProgramStatus.WRITING
                    )
                )
                .ToListAsync();


            return new ApiResponse()
            {
                Message = "Payments found",
                Action = true,
                Result = payments.Select(x => x.ToCoachAllPaymentResponseDto())
            };
        }

        public async Task<ApiResponse>  GetPayment(string phoneNumber, int paymentId)
        {
            var payment = await context.Payments
                .Include(p => p.Coach) 
                .Include(p => p.Athlete) // بارگذاری Athlete
                .ThenInclude(a => a!.User)
                .Include(a => a.AthleteQuestion) // بارگذاری User داخل Athlete
                .ThenInclude(I => I!.InjuryArea)
                .Include(w => w.WorkoutProgram)
                .ThenInclude(z => z.ProgramInDays)
                .ThenInclude(z => z.AllExerciseInDays)
                .ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync(p => p.Coach.PhoneNumber == phoneNumber && p.Id == paymentId);
            if (payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };

            var result = payment.ToCoachPaymentResponseDto();
            if (result.WorkoutProgram!.ProgramInDays.Count == 0)
            {
                result.WorkoutProgram.ProgramInDays.Add(new ProgramInDayDto()
                {
                    ForWhichDay = 1,
                    AllExerciseInDays = []
                });
            }

            return new ApiResponse()
            {
                Message = "Payment found",
                Action = true,
                Result = result
            };
        }


        public async Task<ApiResponse> GetProfile(string phoneNumber)
        {
            var user = await context.Users
                .Include(u => u.Coach)
                .ThenInclude(c => c.CoachingServices)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user?.Coach == null) return new ApiResponse { Action = false, Message = "Coach not found" };
            var coachingService = user.Coach.CoachingServices.Where(x => x.IsDeleted == false).ToList();
            var coachingServiceDto = coachingService.Select(x => x.ToCoachingServiceResponse()).ToList();
            var payments = await context.Payments.Include(p => p.Athlete).ThenInclude(u => u.User).OrderBy(c=>c.PaymentDate)
                .Include(p => p.WorkoutProgram).Where(p =>
                    p.CoachId == user.Coach.Id && p.WorkoutProgram != null &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.WRITING &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.NOTSTARTED)
            .ToListAsync();
            return new ApiResponse
            {
                Action = true, Message = "Coach found",
                Result = user.ToCoachProfileResponseDto(coachingServiceDto, payments)
            };
        }

        public async Task<ApiResponse> SaveWorkoutProgram(string phoneNumber, int paymentId,
            WorkoutProgramDto workoutProgramDto)
        {
            try
            {
                var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
                if (coach == null) return new ApiResponse { Action = false, Message = "Coach not found" };
                var workoutProgram = await context.WorkoutPrograms.Include(x => x.ProgramInDays)
                    .ThenInclude(z => z.AllExerciseInDays)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.CoachId == coach.Id );
                if (workoutProgram is null) return new ApiResponse { Action = false, Message = "Payment not found" };
                workoutProgram.ProgramInDays = workoutProgramDto.Days.ToListOfProgramInDays();
                workoutProgram.ProgramDuration = workoutProgramDto.Week;
                workoutProgram.GeneralWarmUp = workoutProgramDto.GeneralWarmUp
                    ?.Select(x => (GeneralWarmUp)Enum.Parse(typeof(GeneralWarmUp), x)).ToList() ?? [];
                workoutProgram.ProgramLevel = workoutProgramDto.ProgramLevel;
                if (workoutProgramDto.DedicatedWarmUp is not null)
                {
                    workoutProgram.DedicatedWarmUp =
                        (DedicatedWarmUp)Enum.Parse(typeof(DedicatedWarmUp), workoutProgramDto.DedicatedWarmUp);
                }

                workoutProgram.ProgramPriorities = workoutProgramDto.ProgramPriority
                    .Select(x => (ProgramPriority)Enum.Parse(typeof(ProgramPriority), x.ToUpper())).ToList() ?? [];
                if (workoutProgram.Status == WorkoutProgramStatus.NOTSTARTED)
                {
                    workoutProgram.Status = WorkoutProgramStatus.WRITING;
                }

                if (workoutProgramDto.Publish)
                {
                    workoutProgram.Status = WorkoutProgramStatus.NOTACTIVE;
                    var athlete = await context.Athletes.FirstOrDefaultAsync(a => a.Id == workoutProgram.AthleteId);
                    if (athlete is null)
                    {
                        return new ApiResponse()
                        {
                            Action = false,
                            Message = "athlete not found"
                        };
                    }


                    if (athlete.ActiveWorkoutProgramId == 0)
                    {
                        await context.SaveChangesAsync();
                        await AddTrainingSession(paymentId);
                        workoutProgram.Status = WorkoutProgramStatus.ACTIVE;
                        athlete.ActiveWorkoutProgramId = workoutProgram.Id;
                    }
                }

                await context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = true,
                    Message = "workout program saved"
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("*+*" + e);
                return new ApiResponse()
                {
                    Action = false,
                    Message = e.Message
                };
            }
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
            workoutProgram.TotalSessionCount = numberOfDay;


            for (var day = 1; day <= numberOfDay; day++)
            {
                var index = day % programInDayCount;
                await context.TrainingSessions.AddAsync(new TrainingSession
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
        }

        public async Task<ApiResponse> GetWorkoutProgram(string phoneNumber, int paymentId)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null) return new ApiResponse { Action = false, Message = "Coach not found" };
            var workoutProgram = await context.WorkoutPrograms.Include(x => x.ProgramInDays)
                .ThenInclude(z => z.AllExerciseInDays)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
            if (workoutProgram is null) return new ApiResponse { Action = false, Message = "Payment not found" };

            return new ApiResponse()
            {
                Action = true,
                Message = "workout program found",
                Result = workoutProgram.ProgramInDays.ToProgramInDayDto()
            };
        }

        public async Task<ApiResponse> GetCoachDashboard(string phoneNumber)
        {
            try
            {
                var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
                if (coach == null)
                {
                    return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
                }

                var successfulPayments = await context.Payments
                    .Where(p => p.CoachId == coach.Id && p.PaymentStatus == PaymentStatus.SUCCESS)
                    .Include(p => p.WorkoutProgram)
                    .Include(p => p.Athlete)
                    .ToListAsync();

                if (!successfulPayments.Any())
                {
                    return new ApiResponse
                    {
                        Action = true, Message = "گزارشی برای نمایش وجود ندارد.", Result = new CoachDashboardDto
                        {
                            MonthlyIncome = new List<DailyIncomeDto>(),
                            AthleteStats = new AthleteStatsDto(),
                            DailySessionCountChart = new List<DailySessionCountDto>()
                        }
                    };
                }

                var totalSales = successfulPayments.Sum(p => p.Amount);
                var totalTransactions = successfulPayments.Count;
                var totalPrograms = successfulPayments.Count(p => p.WorkoutProgram != null);
                var allCoachAthleteIds = successfulPayments.Select(p => p.AthleteId).Distinct().ToList();
                var totalAthletes = allCoachAthleteIds.Count;

                var activePrograms = successfulPayments
                    .Where(p => p.WorkoutProgram?.Status == WorkoutProgramStatus.ACTIVE)
                    .Select(p => p.WorkoutProgram)
                    .ToList();

                var activeAthletesCount = activePrograms.Select(wp => wp.AthleteId).Distinct().Count();

                var athleteStats = new AthleteStatsDto
                {
                    ActiveAthletes = activeAthletesCount,
                    InactiveAthletes = totalAthletes - activeAthletesCount,
                    NeedsFollowUp = activePrograms.Count(p =>
                        p.LastExerciseDate < DateTime.Now.Date.AddDays(-4) || p.LastExerciseDate == null),
                    NearingCompletion = activePrograms.Count(p =>
                        (p.TotalSessionCount - p.CompletedSessionCount) < 5 &&
                        (p.TotalSessionCount - p.CompletedSessionCount) > 0)
                };

                var pc = new PersianCalendar();
                var today = DateTime.Now;
                var currentYear = pc.GetYear(today);
                var currentMonth = pc.GetMonth(today);
                var monthStartDate = pc.ToDateTime(currentYear, currentMonth, 1, 0, 0, 0, 0);
                var monthEndDate = monthStartDate.AddMonths(1);

                var monthlyIncome = successfulPayments
                    .Where(p => p.PaymentDate >= monthStartDate && p.PaymentDate < monthEndDate)
                    .GroupBy(p => p.PaymentDate.Date)
                    .Select(g => new { DayOfMonth = pc.GetDayOfMonth(g.Key), Amount = g.Sum(p => p.Amount) })
                    .OrderBy(x => x.DayOfMonth)
                    .Select(x => new DailyIncomeDto { Day = x.DayOfMonth.ToString(), Amount = x.Amount })
                    .ToList();


                var rawSessionData = await context.Activities
                    .Where(a => allCoachAthleteIds.Contains(a.AthleteId) &&
                                a.ActivityCategory == ActivityCategory.EXERCISE &&
                                a.Date >= monthStartDate && a.Date < monthEndDate)
                    .GroupBy(a => a.Date)
                    .Select(g => new
                    {
                        ActivityDate = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var dailySessionCountChart = rawSessionData
                    .Select(r => new DailySessionCountDto
                    {
                        Day = pc.GetDayOfMonth(r.ActivityDate).ToString(),
                        Count = r.Count
                    })
                    .OrderBy(d => int.Parse(d.Day))
                    .ToList();


                var dashboardDto = new CoachDashboardDto
                {
                    TotalSales = totalSales,
                    TotalTransactions = totalTransactions,
                    MonthlyIncome = monthlyIncome,
                    TotalPrograms = totalPrograms,
                    TotalAthletes = totalAthletes,
                    AthleteStats = athleteStats,
                    DailySessionCountChart = dailySessionCountChart
                };

                return new ApiResponse { Action = true, Message = "گزارش با موفقیت دریافت شد.", Result = dashboardDto };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] An exception occurred in GetCoachDashboard: {ex.ToString()}");
                return new ApiResponse { Action = false, Message = $"An internal server error occurred: {ex.Message}" };
            }
        }

        public async Task<ApiResponse> GetMonthlyIncomeChart(string phoneNumber, int year, int month)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            var pc = new PersianCalendar();
            DateTime monthStartDate;
            try
            {
                monthStartDate = pc.ToDateTime(year, month, 1, 0, 0, 0, 0);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new ApiResponse { Action = false, Message = "سال یا ماه شمسی نامعتبر است." };
            }

            var monthEndDate = monthStartDate.AddMonths(1);


            var rawIncomeData = await context.Payments
                .Where(p => p.CoachId == coach.Id &&
                            p.PaymentStatus == PaymentStatus.SUCCESS &&
                            p.PaymentDate >= monthStartDate && p.PaymentDate < monthEndDate)
                .GroupBy(p => p.PaymentDate.Date) // گروه‌بندی بر اساس تاریخ
                .Select(g => new
                {
                    PaymentDate = g.Key,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToListAsync(); // <-- اجرای کوئری و انتقال نتایج به حافظه

            var monthlyIncome = rawIncomeData
                .Select(r => new DailyIncomeDto
                {
                    Day = pc.GetDayOfMonth(r.PaymentDate).ToString(),
                    Amount = r.TotalAmount
                })
                .OrderBy(d => int.Parse(d.Day)) // حالا این مرتب‌سازی روی لیست در حافظه انجام می‌شود و صحیح است
                .ToList();

            return new ApiResponse
                { Action = true, Message = "نمودار درآمد ماهانه با موفقیت دریافت شد.", Result = monthlyIncome };
        }

        public async Task<ApiResponse> UpdateSocialMediaLink(string phoneNumber, SocialMediaLinkDto socialMediaLinkDto)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            coach.WhatsApp = socialMediaLinkDto.WhatsApp;
            coach.TelegramLink = socialMediaLinkDto.TelegramLink;
            coach.InstagramLink = socialMediaLinkDto.InstagramLink;
            await context.SaveChangesAsync();

            return new ApiResponse { Action = true, Message = "لینک ها تغییر پیدا کرد" };
        }

        public async Task<ApiResponse> GetSocialMediaLink(string phoneNumber)
        {
            var coach = await context.Coaches.AsNoTracking().Where(c => c.PhoneNumber == phoneNumber).Select(coach =>
                new
                {
                    coach.WhatsApp,
                    coach.TelegramLink,
                    coach.InstagramLink
                }).FirstOrDefaultAsync();
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            return new ApiResponse
            {
                Action = true, Message = "دریافت لینک ها",
                Result = coach
            };
        }


        public async Task<ApiResponse> GetAthletesWithStatus(string coachPhoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            // واکشی تمام پرداخت‌های موفق که به این مربی و یک برنامه تمرینی متصل هستند
            var payments = await context.Payments
                .Where(p => p.CoachId == coach.Id && p.PaymentStatus == PaymentStatus.SUCCESS &&
                            p.WorkoutProgram != null)
                .Include(p => p.Athlete).ThenInclude(a => a.User)
                .Include(p => p.WorkoutProgram)
                .ToListAsync();

            var athletes = payments
                .GroupBy(p => p.Athlete)
                .Select(g => g.Key)
                .ToList();

            var athleteDtos = new List<AthleteStatusDto>();

            foreach (var athlete in athletes)
            {
                var user = athlete.User;


                var relevantProgram = payments
                    .Where(p => p.AthleteId == athlete.Id)
                    .Select(p => p.WorkoutProgram)
                    .OrderByDescending(wp => wp.Status == WorkoutProgramStatus.ACTIVE) // اولویت با فعال
                    .ThenByDescending(wp => wp.StartDate) // سپس جدیدترین
                    .FirstOrDefault();

                if (relevantProgram == null) continue;

                var statusInfo = GetStatus(relevantProgram);

                athleteDtos.Add(new AthleteStatusDto
                {
                    AthleteId = athlete.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = user.ImageProfile,
                    Status = statusInfo,
                    Service = relevantProgram.Title,
                    LastWorkout = relevantProgram.LastExerciseDate.ToString() ?? ""
                });
            }

            return new ApiResponse
                { Action = true, Message = "لیست شاگردان با موفقیت دریافت شد.", Result = athleteDtos };
        }

        public async Task<ApiResponse> GetTransactions(string coachPhoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            var payments = await context.Payments
                .Where(p => p.CoachId == coach.Id && p.PaymentStatus == PaymentStatus.SUCCESS)
                .Include(p => p.Athlete.User)
                .Include(p => p.WorkoutProgram)
                .Include(p => p.CoachService)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var coachPayout = await context.CoachPayouts.Where(p => p.CoachId == coach.Id)
                .OrderByDescending(p => p.RequestDate).ToListAsync();

            var pendingPayout = coachPayout.Find(c => c.Status == PayoutStatus.Pending)?.ToCoachPayoutDto();


            var coachPayoutDto = coachPayout.Select(c => c.ToCoachPayoutDto()).ToList();
            var coachAmount = coach.Amount;

            var transactionDto = payments.Select(p =>
            {
                var programStatus = (p.WorkoutProgram != null &&
                                     p.WorkoutProgram.Status != WorkoutProgramStatus.WRITING &&
                                     p.WorkoutProgram.Status != WorkoutProgramStatus.NOTSTARTED)
                    ? "طراحی شده"
                    : "طراحی نشده";

                return new TransactionDto
                {
                    Amount = $"{p.Amount:N0} ریال", // فرمت‌بندی عدد به همراه جداکننده هزارگان
                    Type = "افزایش",
                    Date = p.PaymentDate.ToString(CultureInfo.CurrentCulture),
                    Description = $"خرید سرویس {p.CoachService.Title}",
                    BuyerName = p.Athlete?.User != null
                        ? $"{p.Athlete.User.FirstName} {p.Athlete.User.LastName}"
                        : "نامشخص",
                    ReferenceId = p.RefId.ToString(), // شناسه خرید یا همان RefId
                    ProgramStatus = programStatus,
                    AppFee = p.AppFee
                    
                };
            }).ToList();

            return new ApiResponse
            {
                Action = true, Message = "لیست تراکنش‌ها با موفقیت دریافت شد.", Result = new
                {
                    coachAmount,
                    pendingPayout,
                    transactionDto,
                    coachPayoutDto
                }
            };
        }


        private static string GetStatus(WorkoutProgram program)
        {
            if (program.Status != WorkoutProgramStatus.ACTIVE)
            {
                return ("Inactive");
            }


            if (program.LastExerciseDate == null || program.LastExerciseDate < DateTime.Now.Date.AddDays(-4))
            {
                return ("NeedsFollowUp");
            }


            if ((program.TotalSessionCount > 0) && (program.TotalSessionCount - program.CompletedSessionCount) < 5)
            {
                return ("NearingCompletion");
            }


            return ("Active");
        }

        public async Task<ApiResponse> CreatePayoutRequest(string coachPhoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            var coachPayout =
                await context.CoachPayouts.FirstOrDefaultAsync(cp =>
                    cp.CoachId == coach.Id && cp.Status == PayoutStatus.Pending);
            if (coachPayout is not null)
            {
                return new ApiResponse { Action = false, Message = "شما یک تسویه در حال انجام دارید " };

            }

            if (coach.Amount < 50000)
            {
                return new ApiResponse { Action = false, Message = "موجودی شما کمتر از مقدار مجاز برای برداشت است " };

            }

            var coachAmount = coach.Amount - 10000;

            var payoutRequest = new CoachPayout()
            {
                CoachId = coach.Id,
                Coach = coach,
                Amount = coachAmount,
                Status = PayoutStatus.Pending,
                RequestDate = DateTime.UtcNow
            };

            await context.CoachPayouts.AddAsync(payoutRequest);
            await context.SaveChangesAsync();

            return new ApiResponse { Action = true, Message = "درخواست شما با موفقیت ثبت شد و در حال بررسی است." };
        }

        public async Task<ApiResponse> GetFaq()
        {
            var getFaq = await context.CoachFaq.ToListAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "get faq",
                Result = getFaq
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
}