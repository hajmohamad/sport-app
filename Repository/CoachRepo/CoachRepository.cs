using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Actions.CouchExercise;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Repository.CoachRepo
{
    public class CoachRepository(ApplicationDbContext context, ISmsService smsService, ILiaraStorage liaraStorage,ITokenService token,ICalculator calculator) : ICoachRepository
    {
        public async Task<ApiResponse> AthleteReportForCoach(int athleteId)
        {
            var athlete = await context.Athletes.Include(u => u.User)
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
                    Height = athlete.Height,
                    Name = athlete.User.FirstName + " " + athlete.User.LastName
                }
            };
        }

        public async Task<ApiResponse> AthleteMonthlyActivityForCoach(int athleteId, int year, int month)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(month);
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.Id == athleteId);
            if (athlete is null)
                return new ApiResponse() { Message = "User is not an athlete", Action = false };

            var persianCalendar = new PersianCalendar();

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

            var publicDiscountValidation = ValidatePublicDiscount(addCoachingServiceDto);
            if (!publicDiscountValidation.Action)
            {
                return publicDiscountValidation;
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

            var publicDiscountValidation = ValidatePublicDiscount(addCoachingServices);
            if (!publicDiscountValidation.Action)
            {
                return publicDiscountValidation;
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
                newCoachService.UsageLimit = coachingService.UsageLimit;
                newCoachService.NumberOfSellWithDiscount = coachingService.NumberOfSellWithDiscount;
                coach.CoachingServices.Add(newCoachService);
                await context.CoachServices.AddAsync(newCoachService);
                await context.SaveChangesAsync(); 

                var discountCodes = await context.DiscountCodes
                    .Where(c => !c.IsDeleted && c.CoachServicesId != null && c.CoachServicesId.Contains(id))
                    .ToListAsync();

                foreach (var dc in discountCodes)
                {
                    dc.CoachServicesId ??= [];

                    dc.CoachServicesId.RemoveAll(x => x == id);

                    if (!dc.CoachServicesId.Contains(newCoachService.Id))
                        dc.CoachServicesId.Add(newCoachService.Id);
                }
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

        public async Task<ApiResponse> CreateDiscountCode(int coachId,
            DiscountCodeCreateDto discountCodeCreateDto)
        {
            var coach = await context.Coaches
                .Include(x => x.CoachingServices)
                .FirstOrDefaultAsync(x => x.Id == coachId);
            if (coach is null)
            {
                return new ApiResponse { Action = false, Message = "User is not a coach" };
            }

        
            var validation = await ValidateDiscountCodeInput(coach,discountCodeCreateDto.Code,
                discountCodeCreateDto.DiscountPercent, discountCodeCreateDto.UsageLimit,
                discountCodeCreateDto.ExpiresAt, null,discountCodeCreateDto.CoachServiceId);
            if (!validation.Action)
            {
                return validation;
            }


            var discountCode = new DiscountCode
            {
                CoachId = coach.Id,
                Coach = coach,
                Code = discountCodeCreateDto.Code,
                DiscountPercent = discountCodeCreateDto.DiscountPercent,
                UsageLimit = discountCodeCreateDto.UsageLimit,
                ExpiresAt = discountCodeCreateDto.ExpiresAt,
                Status = discountCodeCreateDto.ExpiresAt.HasValue && discountCodeCreateDto.ExpiresAt.Value <= DateTime.UtcNow
                    ? DiscountCodeStatus.EXPIRED
                    : DiscountCodeStatus.ACTIVE,
                CoachServicesId = discountCodeCreateDto.CoachServiceId
            };

            await context.DiscountCodes.AddAsync(discountCode);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "کد تخفیف با موفقیت ساخته شد.",
                Result = discountCode.ToDiscountCodeListItemDto(null)
            };
        }

        public async Task<ApiResponse> UpdateDiscountCode(int coachId, int discountCodeId,
            DiscountCodeUpdateDto discountCodeUpdateDto)
        {
            var discountCode = await context.DiscountCodes
                .Include(x => x.Coach).ThenInclude(c=>c.CoachingServices)
                .FirstOrDefaultAsync(x => x.Id == discountCodeId && !x.IsDeleted && x.CoachId==coachId);
            if (discountCode is null)
            {
                return new ApiResponse { Action = false, Message = "کد تخفیف یافت نشد." };
            }

            var validation = await ValidateDiscountCodeInput(discountCode.Coach,discountCodeUpdateDto.Code, 
                discountCodeUpdateDto.DiscountPercent, discountCodeUpdateDto.UsageLimit,
                discountCodeUpdateDto.ExpiresAt, discountCode.Id,
                discountCodeUpdateDto.CoachServiceId);
            if (!validation.Action)
            {
                return validation;
            }

            discountCode.Code = validation.Result as string ?? discountCode.Code;
            discountCode.DiscountPercent = discountCodeUpdateDto.DiscountPercent;
            discountCode.UsageLimit = discountCodeUpdateDto.UsageLimit;
            discountCode.ExpiresAt = discountCodeUpdateDto.ExpiresAt;
            discountCode.UpdatedAt = DateTime.UtcNow;
            discountCode.CoachServicesId = discountCode.CoachServicesId;

            
            if (!string.IsNullOrWhiteSpace(discountCodeUpdateDto.Status))
            {
                if (!Enum.TryParse<DiscountCodeStatus>(discountCodeUpdateDto.Status, true, out var status))
                {
                    return new ApiResponse { Action = false, Message = "وضعیت کد تخفیف نامعتبر است." };
                }

                discountCode.Status = status;
            }

            if (discountCode.ExpiresAt.HasValue && discountCode.ExpiresAt.Value <= DateTime.UtcNow)
            {
                discountCode.Status = DiscountCodeStatus.EXPIRED;
            }

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "کد تخفیف با موفقیت ویرایش شد.",
                Result = discountCode.ToDiscountCodeListItemDto(null)
            };
        }
        public async Task<ApiResponse> GetDiscountCodes(int coachId)
        {
       
            var coach = await context.Coaches.FirstOrDefaultAsync(x => x.Id == coachId);
            if (coach is null)
            {
                return new ApiResponse { Action = false, Message = "User is not a coach" };
            }

            var discountCodes = await context.DiscountCodes
                .Where(x => x.CoachId == coach.Id  && !x.IsDeleted).ToListAsync();

   

            return new ApiResponse
            {
                Action = true,
                Message = discountCodes.Count == 0 ? "هیچ کد تخفیفی موجود نیست" : "لیست کدهای تخفیف",
                Result = discountCodes.Select(x => x.ToDiscountCodeListItemDto(null)).ToList()
            };
        }

        public async Task<ApiResponse> GetDiscountCodeById(int coachId, int discountCodeId)
        {
            var discountCode = await context.DiscountCodes
                .Include(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == discountCodeId && !x.IsDeleted && x.CoachId == coachId);
            if (discountCode is null)
            {
                return new ApiResponse { Action = false, Message = "کد تخفیف یافت نشد." };
            }

            await SyncExpiredDiscountCodes([discountCode]);
            if (discountCode.CoachServicesId == null)
                return new ApiResponse
                {
                    Action = true,
                    Message = "کد تخفیف",
                    Result = discountCode.ToDiscountCodeListItemDto(null)
                };
            var coachService = await context.CoachServices.Where(c =>
                    !c.IsDeleted && discountCode.CoachServicesId != null &&
                    discountCode.CoachServicesId.Contains(c.Id))
                .ToListAsync();

            var result = coachService.Select(cs => new ServiceForDiscountDto()
            {
                Id = cs.Id,
                Title = cs.Title,
                OriginalPrice = cs.Price,
                DiscountPrice = cs.Price - ((cs.Price * discountCode.DiscountPercent) / 100),
                IsActive = cs.IsActive

            }).ToList();

            return new ApiResponse
            {
                Action = true,
                Message = "کد تخفیف",
                Result = discountCode.ToDiscountCodeListItemDto(result)
            };
        }

        public async Task<ApiResponse> DisableDiscountCode(int coachId, int discountCodeId)
        {
            var discountCode = await context.DiscountCodes
                .Include(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == discountCodeId && !x.IsDeleted && x.CoachId==coachId);
            if (discountCode is null)
            {
                return new ApiResponse { Action = false, Message = "کد تخفیف یافت نشد." };
            }

            discountCode.Status = DiscountCodeStatus.INACTIVE;
            discountCode.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "کد تخفیف غیرفعال شد.",
                Result = discountCode.ToDiscountCodeListItemDto(null)
            };
        }
        
        public async Task<ApiResponse> GetServiceForDiscountCode(int coachId, int discountPercent)
        {
            var coachService = await context.CoachServices.Where(cs => cs.CoachId == coachId && !cs.IsDeleted)
                .ToListAsync();
            if (coachService.Count == 0)
            {
                return new ApiResponse()
                {
                    Message = "service not found",
                    Action = true,
                };
            }

            var result = coachService.Select(cs => new ServiceForDiscountDto()
            {
                Id = cs.Id,
                Title = cs.Title,
                OriginalPrice = cs.Price,
                DiscountPrice = cs.Price - ((cs.Price * discountPercent) / 100),
                IsActive = cs.IsActive

            }).ToList();
            
            return new ApiResponse()
            {
                Message = "service  found",
                Action = true,
                Result = result
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
                .ThenInclude(c => c.User)
                .Include(p => p.Athlete)
                .ThenInclude(a => a.User)
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
                .Include(p => p.Athlete) // بارگذاری Athlete
                .ThenInclude(a => a!.User)
                .Include(a => a.AthleteQuestion) // بارگذاری User داخل Athlete
                .ThenInclude(I => I!.InjuryArea)
                .Include(a=>a.AthleteQuestion.AthleteBodyImage)
                .Include(w => w.WorkoutProgram)
                .ThenInclude(z => z.ProgramInDays)
                .ThenInclude(z => z.AllExerciseInDays)
                .ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync(p => p.Coach.PhoneNumber == phoneNumber && p.Id == paymentId);
            if (payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
                var ear = calculator.BmrCalculator(new BmrRequestDto()
                {
                    ActivityLevel = payment.AthleteQuestion.ActivityLevel,
                    Age = DateTime.Today.Year - payment.Athlete.User.BirthDate.Year
                                              - (payment.Athlete.User.BirthDate.Date > DateTime.Today.AddYears(
                                                  -(DateTime.Today.Year - payment.Athlete.User.BirthDate.Year))
                                                  ? 1
                                                  : 0),
                    Gender = payment.Athlete.User.Gender,
                    HeightCm = payment.Athlete.Height,
                    WeightKg = payment.Athlete.CurrentWeight
                });
            

            var result = payment.ToCoachPaymentResponseDto(token.HashEncode(payment.WorkoutProgram?.Id??0),ear);
            if (result.WorkoutProgram!.ProgramInDays is { Count: 0 })
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
        public async Task<ApiResponse> GetCoachPayments(int coachId, PaymentFilterDto filter) {
   

            var query = context.Payments
                .Include(p => p.Athlete).ThenInclude(a => a.User)
                .Include(p => p.WorkoutProgram)
                .Include(p => p.CoachService)
                .Where(p =>
                    p.CoachId == coachId &&
                    p.PaymentStatus == PaymentStatus.SUCCESS &&
                    p.WorkoutProgram != null &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.WRITING &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.NOTSTARTED &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.UNCOMPLETEDQUESTION
                );

           
            query = filter.SortBy?.ToLower() switch
            {
                "date" => filter.SortDesc ? query.OrderByDescending(x => x.PaymentDate) : query.OrderBy(x => x.PaymentDate),
                "service" => filter.SortDesc ? query.OrderByDescending(x => x.CoachService.Title) : query.OrderBy(x => x.CoachService.Title),
                "amount" => filter.SortDesc ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
                "athlete" => filter.SortDesc ? query.OrderByDescending(x => x.Athlete.User.FirstName) : query.OrderBy(x => x.Athlete.User.FirstName),
                _ => query.OrderByDescending(x => x.PaymentDate)
            };

           
            int skip = (filter.Page - 1) * filter.PageSize;
            int totalCount = await query.CountAsync();

            var payments = await query
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync();

            var dto = payments.Select(p => p.ToCoachAllPaymentResponseDto()).ToList();

            var result = new
            {
                Total = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                Items = dto
            };

            return new ApiResponse
            {
                Action = true,
                Message = "Payments fetched",
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
            var coachingService = user.Coach.CoachingServices.Where(x => !x.IsDeleted).ToList();
            var coachingServiceDto = coachingService.Select(x => x.ToCoachingServiceResponse()).ToList();
            var payments = await context.Payments.Include(p => p.Athlete).ThenInclude(u => u.User)
                .OrderByDescending(c => c.PaymentDate)
                .Include(p => p.WorkoutProgram).Where(p =>
                    p.CoachId == user.Coach.Id && p.PaymentStatus == PaymentStatus.SUCCESS&& p.WorkoutProgram != null &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.WRITING &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.NOTSTARTED&&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.UNCOMPLETEDQUESTION)
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
                var workoutProgram = await context.WorkoutPrograms.Include(p=>p.Payment).Include(x => x.ProgramInDays)
                    .ThenInclude(z => z.AllExerciseInDays)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.CoachId == coach.Id);
                if (workoutProgram is null) return new ApiResponse { Action = false, Message = "Payment not found" };
                workoutProgram.ProgramInDays = workoutProgramDto.Days.ToListOfProgramInDays();
                workoutProgram.ProgramDuration = workoutProgramDto.Week;
           
                workoutProgram.ProgramLevel = (ProgramLevel)Enum.Parse(typeof(ProgramLevel),workoutProgramDto.ProgramLevel);

                workoutProgram.ProgramPriorities = workoutProgramDto.ProgramPriority
                    .Select(x => (ProgramPriority)Enum.Parse(typeof(ProgramPriority), x.ToUpper())).ToList() ?? [];
                if (workoutProgram.Status == WorkoutProgramStatus.NOTSTARTED)
                {
                    workoutProgram.Status = WorkoutProgramStatus.WRITING;
                }

                if (workoutProgramDto.Publish)
                {
                    workoutProgram.Status = WorkoutProgramStatus.NOTACTIVE;
                    workoutProgram.StartDate = DateTime.Now;
                    var athlete = await context.Athletes.Include(u => u.User)
                        .FirstOrDefaultAsync(a => a.Id == workoutProgram.AthleteId);
                    if (athlete is null)
                    {
                        return new ApiResponse()
                        {
                            Action = false,
                            Message = "athlete not found"
                        };
                    }
                    var exerciseIds = workoutProgramDto.Days
                        .SelectMany(d => d.AllExerciseInDays)
                        .Select(ex => ex.Id).ToList();

                    coach.Amount += (workoutProgram.Payment.Amount - workoutProgram.Payment.AppFee);              
                    var athleteImg = await context.AthleteImage
                        .Where(a => a.AthleteQuestionId == workoutProgram.Payment.AthleteQuestionId)
                        .FirstOrDefaultAsync();
                    if (athleteImg?.SideLink != null)
                    {
                            await liaraStorage.RemovePhoto(athleteImg.SideLink);
                    }
                    if (athleteImg?.BackLink != null)
                    {
                        await liaraStorage.RemovePhoto(athleteImg.BackLink);
                    }
                    if (athleteImg?.FrontLink != null)
                    {
                        await liaraStorage.RemovePhoto(athleteImg.FrontLink);
                    }

                    if (athleteImg is not null)
                    {
                         context.AthleteImage.Remove(athleteImg);
                    }
                    

                    await smsService.WorkoutReadySms(athlete.PhoneNumber, athlete.User.FirstName, workoutProgram.Title,token.HashEncode(workoutProgram.Id));

                    var workoutExercises = new LastWorkoutExercise()
                    {
                        CoachId = coach.Id,
                        AthleteId = athlete.Id,
                        WorkoutProgramId = workoutProgram.Id,
                        ExerciseIds = exerciseIds

                    }; 
                    await context.LastWorkoutExercises.AddAsync(workoutExercises);
                    
                    if (athlete.ActiveWorkoutProgramId is null)
                    {
                        await AddTrainingSession(paymentId);
                        workoutProgram.Status = WorkoutProgramStatus.ACTIVE;
                        athlete.ActiveWorkoutProgramId = workoutProgram.Id;
                    }
                }

                await context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = true,
                    Message = "workout program saved",
                    Result = new
                    {
                        pdfLink=$"chaarset.ir/program/{token.HashEncode(workoutProgram.Id)}"
                    }
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
            try
            {
                var workoutProgram = await context.WorkoutPrograms
                    .Include(p => p.Payment)
                    .ThenInclude(p => p.AthleteQuestion)
                    .Include(p => p.ProgramInDays)
                    .ThenInclude(d => d.AllExerciseInDays)
                    .FirstAsync(p => p.PaymentId == paymentId);

                if (workoutProgram.Payment.AthleteQuestion != null)
                {
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
            }
            catch (Exception e)
            {
                Console.WriteLine("*+*" + paymentId+"------"+e.Message);
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

                var coachAmount = coach.Amount;

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
                {   CoachAmount= coachAmount,
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
            coach.BaleUserName = socialMediaLinkDto.BaleUserName;
            coach.EitaaUserName = socialMediaLinkDto.EitaaUserName;
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
                    coach.InstagramLink,
                    coach.BaleUserName,
                    coach.EitaaUserName
                    
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
                    Amount = p.Amount,
                    Type = "افزایش",
                    Date = p.PaymentDate.ToString(CultureInfo.CurrentCulture),
                    Description = $"خرید سرویس {p.CoachService.Title}",
                    BuyerName = p.Athlete?.User != null
                        ? $"{p.Athlete.User.FirstName} {p.Athlete.User.LastName}"
                        : "نامشخص",
                    ReferenceId = p.RefId.ToString(),
                    ProgramStatus = programStatus,
                    OriginalAmount = p.OriginalAmount,
                    CodeDiscountAmount = p.CodeDiscountAmount,
                    PublicDiscountAmount = p.PublicDiscountAmount,
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
                return new ApiResponse { Action = false, Message = "حداقل موجودی قابل برداشت  ۵۰ هزارتومان است" };
            }

            var coachAmount = coach.Amount - 10000;

            var payoutRequest = new CoachPayout()
            {
                CoachId = coach.Id,
                Coach = coach,
                Amount = coachAmount,
                Status = PayoutStatus.Pending,
                RequestDate = DateTime.Now
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

       

        public Task<ApiResponse> GetWPkey(int workoutProgramId)
        {
            try
            {
                return Task.FromResult(new ApiResponse()
                {
                    Action = true,
                    Message = "getwpkey",
                    Result = new
                    {
                        wpkey = token.HashEncode(workoutProgramId)
                    }

                });
            }
            catch (Exception exception)
            {
                return Task.FromException<ApiResponse>(exception);
            }
        }
        public async Task<ApiResponse> ChoseWorkoutProgramFeedBack(
            string phoneNumber,
            List<ChoseWorkoutProgramFeedBackDto> choseWorkoutProgramFeedBackDtos)
        {
            var coach = await context.Coaches
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            var dtoById = choseWorkoutProgramFeedBackDtos
                .ToDictionary(x => x.Id, x => x.IsShouldBeTrue);

            var feedbacks = await context.WorkoutProgramFeedback
                .Where(e => e.CouchId == coach.Id)
                .ToListAsync();

            if (!feedbacks.Any())
            {
                return new ApiResponse { Action = false, Message = "هیچ فیدبکی برای این مربی یافت نشد." };
            }

            foreach (var fb in feedbacks)
            {
                if (dtoById.TryGetValue(fb.Id, out var shouldBeTrue))
                {
                    fb.IsChosen = shouldBeTrue;
                }
               
            }

            var affectedRows = await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "فیدبک‌ها بروزرسانی شدند",
                Result = affectedRows
            };
        }

        public async Task<(IEnumerable<AllExerciseResponseDto> Exercises, int TotalCount)>
            GetExercisesWithFilterForCoach(
                string? level, string? type, string? mechanic, string?[]? equipment, string? muscle,
                string? place, int page, int pageSize, string? searchTerm, int? athleteId, int couchId)
        {
            var pinnedExerciseIds  = new List<int>();
            if (muscle != null)
            {

               Enum.TryParse<BaseCategory>(muscle, true, out var baseCategory); 

               var coachPins = await context.CoachPineExercises
                                   .Where(p => p.CoachId == couchId && p.BaseCategory == baseCategory)
                      .ToListAsync(); 
               pinnedExerciseIds = coachPins.SelectMany(p => p.ExerciseIds).ToList();
            }

       

            var lastProgramExerciseIds = new List<int>();
            if (athleteId.HasValue)
            {
                var lastWorkout = await context.LastWorkoutExercises
                    .Where(w => w.CoachId == couchId && w.AthleteId == athleteId.Value)
                    .OrderByDescending(w => w.Id) 
                    .FirstOrDefaultAsync();

                if (lastWorkout != null)
                {
                    lastProgramExerciseIds = lastWorkout.ExerciseIds.ToList();
                }
            }

            var query = context.Exercises.AsQueryable();
    
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.PersianName.Contains(searchTerm) || 
                                         e.EnglishName.Contains(searchTerm));
            }

            if (Enum.TryParse<ExerciseLevel>(level, true, out var levelEnum))
                query = query.Where(e => e.ExerciseLevel == levelEnum);

            if (Enum.TryParse<ExerciseType>(type, true, out var typeEnum))
                query = query.Where(e => e.ExerciseType == typeEnum);
        
            if (Enum.TryParse<MechanicType>(mechanic, true, out var mechanicEnum))
                query = query.Where(e => e.Mechanics == mechanicEnum);

            if (equipment != null && equipment.Length != 0)
            {
                var validEquipments = new List<EquipmentType>();

                foreach (var eq in equipment)
                {
                    if (Enum.TryParse<EquipmentType>(eq, true, out var equipEnum))
                        validEquipments.Add(equipEnum);
                }

                if (validEquipments.Any())
                    query = query.Where(e => validEquipments.Contains(e.Equipment));
            }

            if (Enum.TryParse<BaseCategory>(muscle, true, out var muscleEnum))
                query = query.Where(e => e.BaseCategory == muscleEnum);
    
            if (!string.IsNullOrEmpty(place))
                query = query.Where(e => EF.Functions.Like(e.Description, $"%{place}%"));

            var totalCount = await query.CountAsync();

            var exercises = await query
                .Select(e => new 
                {
                    Exercise = e,
                    IsPinned = pinnedExerciseIds.Contains(e.Id),
                    IsInLastProgram = lastProgramExerciseIds.Contains(e.Id)
                })
                .OrderByDescending(x => x.IsPinned) 
                .ThenByDescending(x => x.Exercise.Views) 
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AllExerciseResponseDto
                {
                    Id = x.Exercise.Id,
                    Name = x.Exercise.PersianName,
                    ImageLink = x.Exercise.ImageLink,
                    BaseCategory = x.Exercise.BaseCategory.ToString(),
                    Equipment = x.Exercise.Equipment.ToString(),
                    ExerciseType = x.Exercise.ExerciseType.ToString(),
                    Level = x.Exercise.ExerciseLevel.ToString(),
                    Mechanics = x.Exercise.Mechanics.ToString(),
                    View = x.Exercise.Views,
                    Met = x.Exercise.Met,
                    IsPinned = x.IsPinned,
                    IsInLastProgram = x.IsInLastProgram 
                })
                .ToListAsync();

            return (exercises, totalCount);
}

        public async Task<ApiResponse> AddPineExercise(int exerciseId, int coachId)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.Id == coachId);
            if (coach == null)
                return new ApiResponse()
                {
                    Message = "coach not found",
                    Action = false,
                };
            
            var exercise = await context.Exercises.FirstOrDefaultAsync(e => e.Id == exerciseId);
            if (exercise == null)
                return new ApiResponse()
                {
                    Message = "exercise not found",
                    Action = false,
                };   
            var oldPineExercises = await context.CoachPineExercises.FirstOrDefaultAsync(ex=>ex.CoachId==coach.Id&&ex.BaseCategory ==exercise.BaseCategory);
            if (oldPineExercises == null)
            {
                var newPineCategory = new CoachPineExercise
                {
                    CoachId = coach.Id,
                    BaseCategory = exercise.BaseCategory,
                    ExerciseIds = [exerciseId],

                };
                await context.CoachPineExercises.AddAsync(newPineCategory);
            }
            else
            {
                oldPineExercises.ExerciseIds.Add(exercise.Id);
            }

            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "added",
                Action = true,
            };
        }
        public async Task<ApiResponse> RemovePineExercise(int exerciseId, int coachId)
        {
            var coach = await context.Coaches
                .FirstOrDefaultAsync(c => c.Id == coachId);

            if (coach == null)
            {
                return new ApiResponse
                {
                    Message = "coach not found",
                    Action = false
                };
            }

            var oldPineExercises = await context.CoachPineExercises
                .Where(ex => ex.CoachId == coach.Id)
                .ToListAsync();  

            var target = oldPineExercises
                .FirstOrDefault(ex => ex.ExerciseIds.Contains(exerciseId));

            if (target == null)
            {
                return new ApiResponse
                {
                    Message = "exercise not found",
                    Action = false
                };
            }

            target.ExerciseIds.Remove(exerciseId);

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Message = "remove pine",
                Action = true
            };
        }



        public async Task<ApiResponse> GetWorkoutProgramFeedBack(string phoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            var feedBack = await context.WorkoutProgramFeedback.Where(fb=>fb.CouchId==coach.Id).ToListAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "feedback",
                Result = feedBack
            };
        }

        private DateTime GetLastSaturday(DateTime today)
        {
            var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            return today.AddDays(-diff);
        }

        private ApiResponse ValidatePublicDiscount(AddCoachServiceDto dto)
        {
            if (dto.PublicDiscountPercent is null || dto.PublicDiscountPercent <= 0)
            {
                dto.UsageLimit = null;
                dto.PublicDiscountExpiresAt = null;
                dto.PublicDiscountPercent = null;
                return new ApiResponse
                {
                    Action = true ,
                    Message = "کد تخفیفی ایجاد نشده است"
                };
            }
            var now = DateTime.Now;

         

            if (dto.PublicDiscountExpiresAt.HasValue && dto.PublicDiscountExpiresAt.HasValue &&
                now >= dto.PublicDiscountExpiresAt.Value)
            {
                return new ApiResponse { Action = false, Message = "تاریخ انقضا قبل از تاریخ شروع است" };
            }

            var amount = CoachMappers.CalculateDiscountAmount(dto.Price, dto.PublicDiscountPercent.Value);
            if (amount <= 0)
            {
                return new ApiResponse { Action = false, Message = "مقدار تخفیف عمومی نامعتبر است." };
            }

            return new ApiResponse
            {
                Action = true
                , Message = "کد تخفیف درست است"
            };
        }

        private async Task<ApiResponse> ValidateDiscountCodeInput(Coach coach,string code, double DiscountPercent,
            int? usageLimit, DateTime? expiresAt, int? currentDiscountCodeId,List<int>? coachServiceIdsDto)
        {
            var now  = DateTime.Now;
            
            if (DiscountPercent <= 0)
            {
                return new ApiResponse { Action = false, Message = "مقدار تخفیف باید بیشتر از صفر باشد." };
            }

            if (usageLimit is <= 0)
            {
                return new ApiResponse { Action = false, Message = "سقف استفاده باید بیشتر از صفر باشد." };
            }

            if (coachServiceIdsDto is not null && coachServiceIdsDto.Any())
            {
                var existingCoachServiceIds = new HashSet<int>(
                    coach.CoachingServices
                        .Where(ca => !ca.IsDeleted)
                        .Select(ca => ca.Id)
                );

                var invalidServiceId = coachServiceIdsDto
                    .FirstOrDefault(id => !existingCoachServiceIds.Contains(id));

                if (invalidServiceId != 0)
                {
                    return new ApiResponse
                    {
                        Action = false,
                        Message = "سرویس انتخابی موجود نمی‌باشد"
                    };
                }
            }


            if ( expiresAt.HasValue && now < expiresAt.Value)
            {
                return new ApiResponse { Action = false, Message = "تاریخ شروع باید قبل از تاریخ انقضا باشد." };
            }
            
            var exists = await context.DiscountCodes
                .AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentDiscountCodeId);
        
            if (exists)
            {
                return new ApiResponse { Action = false, Message = "این کد تخفیف قبلاً ثبت شده است." };
            }

            return new ApiResponse { Action = true,
                Message = "کد تخفیف درست است ",
                Result = code };
            
        }

   
       

        private async Task SyncExpiredDiscountCodes(IEnumerable<DiscountCode> discountCodes)
        {
            var shouldSave = false;
            foreach (var discountCode in discountCodes)
            {
                if (discountCode.Status != DiscountCodeStatus.INACTIVE &&
                    discountCode.ExpiresAt.HasValue &&
                    discountCode.ExpiresAt.Value <= DateTime.UtcNow &&
                    discountCode.Status != DiscountCodeStatus.EXPIRED)
                {
                    discountCode.Status = DiscountCodeStatus.EXPIRED;
                    shouldSave = true;
                }
            }

            if (shouldSave)
            {
                await context.SaveChangesAsync();
            }
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
