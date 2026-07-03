using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Coach;
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
    public class CoachRepository(
        ApplicationDbContext context,
        ISmsService smsService,
        IStorage storage,
        ITokenService token,
        ICalculator calculator,
        IExerciseCacheService exerciseCache
        ) : ICoachRepository
    {
        #region websiteurl
        public async Task<ApiResponse> CheckWebSiteUrlAvailabilityAsync(int coachId, string url)
        {
            var checkResult = new WebSiteUrlCheckResultDto();

            if (string.IsNullOrWhiteSpace(url))
            {
                checkResult.IsAvailable = false;
                checkResult.Message = "آدرس وب‌سایت نمی‌تواند خالی باشد.";
                return new ApiResponse
                {
                    Action = false,
                    Result = checkResult,
                    Message = checkResult.Message
                };
            }

            var sanitizedUrl = url.Trim().ToLower();

            if (sanitizedUrl.Length < 3 || sanitizedUrl.Length > 20)
            {
                checkResult.IsAvailable = false;
                checkResult.Message = "آدرس وب‌سایت باید بین ۳ تا ۲۰ کاراکتر باشد.";
                return new ApiResponse
                {
                    Action = false,
                    Result = checkResult,
                    Message = checkResult.Message
                };
            }

            if (!Regex.IsMatch(sanitizedUrl, "^[a-zA-Z0-9-]+$"))
            {
                checkResult.IsAvailable = false;
                checkResult.Message = "فقط حروف انگلیسی، اعداد و خط تیره مجاز هستند.";
                return new ApiResponse
                {
                    Action = false,
                    Result = checkResult,
                    Message = checkResult.Message
                };
            }

            var isDuplicate = await context.Coaches
                .AsNoTracking()
                .AnyAsync(c => c.WebSiteUrl == sanitizedUrl && c.Id != coachId);

            if (isDuplicate)
            {
                checkResult.IsAvailable = false;
                checkResult.Message = "این آدرس قبلاً ثبت شده است.";
                return new ApiResponse
                {
                    Action = false,
                    Result = checkResult,
                    Message = checkResult.Message
                };
            }

            checkResult.IsAvailable = true;
            checkResult.Message = "آدرس آزاد و قابل استفاده است.";

            return new ApiResponse
            {
                Action = true,
                Result = checkResult,
                Message = checkResult.Message
            };
        }

        
            public async Task<ApiResponse> GetWebSiteUrlStatusAsync(int coachId)
        {
            var coach = await context.Coaches
                .AsNoTracking()
                .Where(c => c.Id == coachId)
                .Select(c => new { c.WebSiteUrl, c.WebSiteUrlUpDateTime })
                .FirstOrDefaultAsync();

            if (coach == null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "مربی یافت نشد."
                };
            }

            var status = new CoachWebSiteUrlStatusDto
            {
                WebSiteUrl = coach.WebSiteUrl
            };

            var timePassed = DateTime.UtcNow - coach.WebSiteUrlUpDateTime;
            var daysPassed = timePassed.Days;

            if (string.IsNullOrEmpty(coach.WebSiteUrl))
            {
                status.CanChange = true;
                status.DaysRemaining = 0;
                status.Message = "شما می‌توانید آدرس خود را ثبت کنید.";
            }
            else if (daysPassed >= 14)
            {
                status.CanChange = true;
                status.DaysRemaining = 0;
                status.Message = "امکان تغییر آدرس برای شما فعال است.";
            }
            else
            {
                status.CanChange = false;
                status.DaysRemaining = 14 - daysPassed;
                status.Message = $"شما {status.DaysRemaining} روز دیگر می‌توانید آدرس خود را تغییر دهید.";
            }

            return new ApiResponse
            {
                Action = true,
                Message = "اضافه شد",
                Result = status
            };
        }

        public async Task<ApiResponse> UpdateWebSiteUrlAsync(int coachId, string newUrl)
        {
            newUrl = newUrl.Trim().ToLower();

            if (newUrl.Length < 3 || newUrl.Length > 20)
            {
                return new ApiResponse 
                { 
                    Action = false, 
                    Message = "آدرس وب‌سایت باید بین ۳ تا ۲۰ کاراکتر باشد." 
                };
            }

            var regex = new Regex("^[a-zA-Z0-9-]+$");
            if (!regex.IsMatch(newUrl))
            {
                return new ApiResponse 
                { 
                    Action = false, 
                    Message = "فقط حروف انگلیسی، اعداد و خط تیره مجاز هستند." 
                };
            }

            var isDuplicate = await context.Coaches
                .AnyAsync(c => c.WebSiteUrl == newUrl && c.Id != coachId);

            if (isDuplicate)
            {
                return new ApiResponse 
                { 
                    Action = false, 
                    Message = "این آدرس قبلاً توسط مربی دیگری ثبت شده است. لطفاً نام دیگری انتخاب کنید." 
                };
            }

            var coach = await context.Coaches
                .FirstOrDefaultAsync(c => c.Id == coachId);

            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            if (!string.IsNullOrEmpty(coach.WebSiteUrl))
            {
                var timePassed = DateTime.UtcNow - coach.WebSiteUrlUpDateTime;
                if (timePassed.Days < 14)
                {
                    var daysRemaining = 14 - timePassed.Days;
                    return new ApiResponse 
                    { 
                        Action = false, 
                        Message = $"شما {daysRemaining} روز دیگر می‌توانید آدرس خود را تغییر دهید." 
                    };
                }
            }

            coach.WebSiteUrl = newUrl;
            coach.WebSiteUrlUpDateTime = DateTime.UtcNow;

            context.Coaches.Update(coach);
            await context.SaveChangesAsync();

            return new ApiResponse 
            { 
                Action = true, 
                Message = "آدرس وب‌سایت شما با موفقیت ثبت و تغییر یافت." 
            };
        }
         #endregion
        #region changePhoto
        public async Task<ApiResponse> GetAllChangePhotos(int coachId)
    {
        
        var photos = await context.AthleteChangePhotos
            .Where(x => x.CoachId == coachId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Title, x.Description, x.PhotoUrl, x.CreatedAt })
            .ToListAsync();

        return new ApiResponse
        {
            Action = true,
            Message = "Change photos fetched successfully",
            Result = photos
        };
    }

    public async Task<ApiResponse> GetChangePhotoById(int coachId, int id)
    {
       

        var photo = await context.AthleteChangePhotos
            .Where(x => x.CoachId == coachId && x.Id == id)
            .Select(x => new { x.Id, x.Title, x.Description, x.PhotoUrl, x.CreatedAt })
            .FirstOrDefaultAsync();

        if (photo is null) return new ApiResponse { Message = "Change photo not found", Action = false };

        return new ApiResponse
        {
            Action = true,
            Message = "Change photo fetched successfully",
            Result = photo
        };
    }

    public async Task<ApiResponse> AddChangePhoto(int coachId, IFormFile file, AddAthleteChangePhotoDto dto)
    {
        if (file.Length == 0)
            return new ApiResponse { Message = "File is required", Action = false };

        

        var upload = await storage.UploadImage(file, "","change-photos"); 
        // upload.Url
        if (!upload.Action)
        {
            return new ApiResponse()
            {
                Action = false,
                Message = "File upload failed",
            };

        }

        var entity = new AthleteChangePhoto
        {
            Title = dto.Title,
            Description = dto.Description,
            PhotoUrl = upload.Result.ToString(),
            CoachId = coachId
        };

        await context.AthleteChangePhotos.AddAsync(entity);
        await context.SaveChangesAsync();

        return new ApiResponse
        {
            Action = true,
            Message = "Change photo added successfully",
            Result = new { entity.Id }
        };
    }

    public async Task<ApiResponse> EditChangePhoto(int coachId, IFormFile? file, EditAthleteChangePhotoDto dto)
    {
        
        var entity = await context.AthleteChangePhotos
            .FirstOrDefaultAsync(x => x.CoachId == coachId && x.Id == dto.Id);

        if (entity is null) return new ApiResponse { Message = "Change photo not found", Action = false };

        entity.Title = dto.Title;
        entity.Description = dto.Description;

        if (file != null && file.Length > 0)
        {
            
            var upload = await storage.UploadImage(file,entity.PhotoUrl, "change-photos");
            if (!upload.Action)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "File upload failed",
                };

            }
            entity.PhotoUrl = upload.Result.ToString();

        }

        await context.SaveChangesAsync();

        return new ApiResponse
        {
            Action = true,
            Message = "Change photo updated successfully",
            Result = new { entity.Id }
        };
    }

    public async Task<ApiResponse> DeleteChangePhoto(int coachId, int id)
    {
        var entity = await context.AthleteChangePhotos
            .FirstOrDefaultAsync(x => x.CoachId == coachId && x.Id == id);

        if (entity is null) return new ApiResponse { Message = "Change photo not found", Action = false };
        await storage.RemovePhoto(entity.PhotoUrl);

        context.AthleteChangePhotos.Remove(entity);
        await context.SaveChangesAsync();

        return new ApiResponse
        {
            Action = true,
            Message = "Change photo deleted successfully"
        };
    }
        

        #endregion
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
            //
            // var publicDiscountValidation = ValidatePublicDiscount(addCoachingServiceDto);
            // if (!publicDiscountValidation.Action)
            // {
            //     return publicDiscountValidation;
            // }

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

        public async Task<ApiResponse> DeleteCoachingService(string phoneNumber, int id)
        {
            var coach = await context.Coaches
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null)
                return new ApiResponse { Message = "User is not a coach", Action = false };

            var coachingService =
                await context.CoachServices.FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coach.Id);
            if (coachingService is null)
                return new ApiResponse { Message = "Coaching Service not found", Action = false };

            coachingService.IsDeleted = true;

        
            var affectedDiscountCodes = await context.DiscountCodes
                .Where(dc => !dc.IsDeleted &&
                             dc.CoachId == coach.Id &&
                             dc.DiscountCodeCoachServices.Any(dcs => dcs.CoachServiceId == id))
                .ToListAsync();

            if (affectedDiscountCodes.Any())
            {
                foreach (var discountCode in affectedDiscountCodes)
                {
                   
                    discountCode.Status = DiscountCodeStatus.INACTIVE;
                }
            }

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Message = "Coaching Service deleted successfully",
                Action = true,
                Result = coachingService.ToCoachingServiceResponse()
            };
        }

        public async Task<ApiResponse> UpdateCoachingService(string phoneNumber, int id,
            AddCoachServiceDto addCoachingServices)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null)
                return new ApiResponse { Message = "User is not a coach", Action = false };

            if (addCoachingServices.Price < 50000)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "مبلغ وارد شده نمیتواند کمتر از 50 هزار تومان باشد"
                };
            }

            var coachingService = await context.CoachServices
                .FirstOrDefaultAsync(x => x.Id == id && x.CoachId == coach.Id && !x.IsDeleted);

            if (coachingService is null)
            {
                return new ApiResponse { Message = "سرویس کوچینگ یافت نشد یا قبلاً حذف شده است.", Action = false };
            }

            var hasActivePayments = await context.Payments.AnyAsync(p => p.CoachServiceId == coachingService.Id);

            if (hasActivePayments)
            {
                var discountLinksToMigrate = await context.DiscountCodeCoachServices
                    .Where(dcs => dcs.CoachServiceId == id)
                    .ToListAsync();

                coachingService.IsDeleted = true;

                var newCoachService = addCoachingServices.ToCoachService(coach);
                newCoachService.NumberOfSell = coachingService.NumberOfSell;

                await context.CoachServices.AddAsync(newCoachService);
                await context.SaveChangesAsync();

                if (discountLinksToMigrate.Any())
                {
                    foreach (var link in discountLinksToMigrate)
                    {
                        link.CoachServiceId = newCoachService.Id;
                    }
                }
            }
            else
            {
                coachingService.UpdateCoachServices(addCoachingServices);
            }

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Message = "Coaching Service updated successfully",
                Action = true,
                Result = coachingService.ToCoachingServiceResponse() // همچنان سرویس قدیمی را برمی‌گردانیم
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


        #region DiscountCode

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

            var validation = await ValidateDiscountCodeInput(coach, discountCodeCreateDto.Code,
                discountCodeCreateDto.DiscountPercent, discountCodeCreateDto.UsageLimit,
                discountCodeCreateDto.ExpiresAt, null, discountCodeCreateDto.CoachServiceId,discountCodeCreateDto.AppliesToAllServices);
            if (!validation.Action)
            {
                return validation;
            }

            var discountCode = new DiscountCode
            {
                CoachId = coach.Id,
                Code = discountCodeCreateDto.Code,
                DiscountPercent = discountCodeCreateDto.DiscountPercent,
                UsageLimit = discountCodeCreateDto.UsageLimit,
                ExpiresAt = discountCodeCreateDto.ExpiresAt,
                Status = discountCodeCreateDto.ExpiresAt.HasValue &&
                         discountCodeCreateDto.ExpiresAt.Value <= DateTime.UtcNow
                    ? DiscountCodeStatus.EXPIRED
                    : DiscountCodeStatus.ACTIVE,
                AppliesToAllServices = discountCodeCreateDto.AppliesToAllServices
            };

            if (discountCodeCreateDto.CoachServiceId != null && discountCodeCreateDto.CoachServiceId.Count != 0)
            {
                foreach (var serviceId in discountCodeCreateDto.CoachServiceId)
                {
                    discountCode.DiscountCodeCoachServices.Add(new DiscountCodeCoachService
                    {
                        CoachServiceId = serviceId
                    });
                }
            }
            else
            {
                discountCode.AppliesToAllServices = true;
            }

            await context.DiscountCodes.AddAsync(discountCode);
            await context.SaveChangesAsync();
            

           
            

            return new ApiResponse
            {
                Action = true,
                Message = "کد تخفیف با موفقیت ساخته شد."
            };
        }

        public async Task<ApiResponse> UpdateDiscountCode(int coachId, int discountCodeId,
            DiscountCodeUpdateDto discountCodeUpdateDto)
        {
            var discountCode = await context.DiscountCodes
                .Include(dc => dc.Coach)
                .ThenInclude(dc=>dc.CoachingServices)
                .Include(dc => dc.DiscountCodeCoachServices) 
                .FirstOrDefaultAsync(x => x.Id == discountCodeId && !x.IsDeleted && x.CoachId == coachId);

            if (discountCode is null)
            {
                return new ApiResponse { Action = false, Message = "کد تخفیف یافت نشد." };
            }

            var validation = await ValidateDiscountCodeInput(discountCode.Coach, discountCodeUpdateDto.Code,
                discountCodeUpdateDto.DiscountPercent, discountCodeUpdateDto.UsageLimit,
                discountCodeUpdateDto.ExpiresAt, discountCode.Id,
                discountCodeUpdateDto.CoachServiceId,discountCode.AppliesToAllServices);
            if (!validation.Action)
            {
                return validation;
            }

            discountCode.Code = validation.Result as string ?? discountCode.Code;
            discountCode.DiscountPercent = discountCodeUpdateDto.DiscountPercent;
            discountCode.UsageLimit = discountCodeUpdateDto.UsageLimit;
            discountCode.ExpiresAt = discountCodeUpdateDto.ExpiresAt;
            discountCode.UpdatedAt = DateTime.UtcNow;

            var existingServiceIds = discountCode.DiscountCodeCoachServices.Select(s => s.CoachServiceId).ToList();
            var newServiceIds = discountCodeUpdateDto.CoachServiceId ?? new List<int>();

            var servicesToRemove = discountCode.DiscountCodeCoachServices
                .Where(s => !newServiceIds.Contains(s.CoachServiceId))
                .ToList();
            context.RemoveRange(servicesToRemove); 

            var serviceIdsToAdd = newServiceIds.Except(existingServiceIds).ToList();
            foreach (var serviceId in serviceIdsToAdd)
            {
                discountCode.DiscountCodeCoachServices.Add(new DiscountCodeCoachService
                {
                    CoachServiceId = serviceId
                });
            }

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
            };
        }

        public async Task<ApiResponse> GetDiscountCodes(int coachId)
        {
            var discountCodesData = await context.DiscountCodes
                .Where(x => x.CoachId == coachId && !x.IsDeleted)
                .Select(dc => new 
                {
                    DiscountCode = dc,
                    ServiceNames = dc.DiscountCodeCoachServices
                        .Select(dcs => new ServiceForDiscountDto()
                        {
                            Title = dcs.CoachService.Title
                        })
                        .ToList()
                })
                .ToListAsync();

            var discountCodes = discountCodesData.Select(x => x.DiscountCode).ToList();
            await SyncExpiredDiscountCodes(discountCodes);

            var result = discountCodesData.Select(x => 
                x.DiscountCode.ToDiscountCodeListItemDto(x.ServiceNames.Count != 0 ? x.ServiceNames : null)
            ).ToList();

            return new ApiResponse
            {
                Action = true,
                Message = discountCodes.Count == 0 ? "هیچ کد تخفیفی موجود نیست" : "لیست کدهای تخفیف",
                Result = result
            };

        }
        public async Task<ApiResponse> GetAllServicesWithCalculatedDiscount(int coachId, int percent)
        {
            if (percent is < 0 or > 100)
                return new ApiResponse { Action = false, Message = "درصد وارد شده باید بین 0 تا 100 باشد." };

            var allCoachServices = await context.CoachServices
                .Where(cs => cs.CoachId == coachId && !cs.IsDeleted)
                .ToListAsync();

            if (!allCoachServices.Any())
            {
                return new ApiResponse 
                { 
                    Action = true, 
                    Message = "سرویسی برای این مربی یافت نشد.", 
                    Result = new List<ServiceForDiscountDto>() 
                };
            }

            var result = allCoachServices.Select(cs => new ServiceForDiscountDto
            {
                Id = cs.Id,
                Title = cs.Title,
                OriginalPrice = cs.Price,
                DiscountPrice = cs.Price - ((cs.Price * percent) / 100),
                IsActive = cs.IsActive,
            }).ToList();

            return new ApiResponse
            {
                Action = true,
                Message = $"لیست سرویس‌ها با محاسبه تخفیف {percent} درصد",
                Result = result
            };
        }


        public async Task<ApiResponse> GetDiscountCodeById(int coachId, int discountCodeId)
        {
            var discountCode = await context.DiscountCodes
                .Include(dc => dc.DiscountCodeCoachServices)
                .FirstOrDefaultAsync(x => x.Id == discountCodeId && !x.IsDeleted && x.CoachId == coachId);

            if (discountCode is null)
            {
                return new ApiResponse { Action = false, Message = "کد تخفیف یافت نشد." };
            }

            await SyncExpiredDiscountCodes([discountCode]);

            var allCoachServices = await context.CoachServices
                .Where(cs => cs.CoachId == coachId && !cs.IsDeleted)
                .ToListAsync();

            var selectedServiceIds = discountCode.DiscountCodeCoachServices
                .Select(dcs => dcs.CoachServiceId)
                .ToList();

            var services = allCoachServices.Select(cs => 
            {
                var isSelected = selectedServiceIds.Contains(cs.Id);
        
                return new ServiceForDiscountDto
                {
                    Id = cs.Id,
                    Title = cs.Title,
                    OriginalPrice = cs.Price,
                    DiscountPrice = isSelected 
                        ? cs.Price - ((cs.Price * discountCode.DiscountPercent) / 100) 
                        : cs.Price,
                    IsActive = cs.IsActive,
                    IsChoose = isSelected 
                };
            }).ToList();

            return new ApiResponse
            {
                Action = true,
                Message = "جزئیات کد تخفیف به همراه وضعیت تمامی سرویس‌ها",
                Result = discountCode.ToDiscountCodeListItemDto(services)
            };
        }

        public async Task<ApiResponse> ChangeStatusForDiscountCode(int coachId, int discountCodeId, string status)
        {
            var discountCode = await context.DiscountCodes
                .Include(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == discountCodeId && !x.IsDeleted && x.CoachId == coachId);
            if (discountCode is null)
            {
                return new ApiResponse { Action = false, Message = "کد تخفیف یافت نشد." };
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<DiscountCodeStatus>(status, true, out var statusEnum))
                {
                    return new ApiResponse { Action = false, Message = "وضعیت کد تخفیف نامعتبر است." };
                }

                discountCode.Status = statusEnum;
            }

            discountCode.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = $"وضعیت کد تخفیف به {discountCode.Status} تغییر کرد",
                Result = discountCode.ToDiscountCodeListItemDto(null)
            };
        }

        #endregion


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

        public async Task<ApiResponse> GetPayment(string phoneNumber, int paymentId)
        {
            var payment = await context.Payments
                .Include(p => p.Athlete) // بارگذاری Athlete
                .ThenInclude(a => a!.User)
                .Include(a => a.AthleteQuestion) // بارگذاری User داخل Athlete
                .ThenInclude(I => I!.InjuryArea)
                .Include(a => a.AthleteQuestion.AthleteBodyImage)
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


            var result = payment.ToCoachPaymentResponseDto(token.HashEncode(payment.WorkoutProgram?.Id ?? 0), ear);
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

        public async Task<ApiResponse> GetCoachPayments(int coachId, PaymentFilterDto filter)
        {
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
                "date" => filter.SortDesc
                    ? query.OrderByDescending(x => x.PaymentDate)
                    : query.OrderBy(x => x.PaymentDate),
                "service" => filter.SortDesc
                    ? query.OrderByDescending(x => x.CoachService.Title)
                    : query.OrderBy(x => x.CoachService.Title),
                "amount" => filter.SortDesc ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
                "athlete" => filter.SortDesc
                    ? query.OrderBy(x => x.Athlete.User.FirstName)
                    : query.OrderByDescending(x => x.Athlete.User.FirstName),
                _ => query.OrderByDescending(x => x.PaymentDate)
            };


            var skip = (filter.Page - 1) * filter.PageSize;
            var totalCount = await query.CountAsync();

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

        #region Cardnumber

        public async Task<ApiResponse> AddCardNumber(int coachId, AddCardNumberDto addCardNumberDto)
        {
            var cardNumber = await context.CoachCardNumbers.Where(c => c.CoachId == coachId).FirstOrDefaultAsync();
            if (cardNumber is null)
            {
                cardNumber = new CoachCardNumber()
                {
                    CardName = addCardNumberDto.CardName,
                    ShebaNumber = addCardNumberDto.ShebaNumber,
                    CoachId = coachId
                };
                await context.CoachCardNumbers.AddAsync(cardNumber);
                await context.SaveChangesAsync();
                return new ApiResponse
                {
                    Action = true, Message = "اضافه شد"
                };
            }

            cardNumber.CardName = addCardNumberDto.CardName;
            cardNumber.ShebaNumber = addCardNumberDto.ShebaNumber;
            await context.SaveChangesAsync();
            return new ApiResponse
            {
                Action = true, Message = "ادیت  شد"
            };
        }

        public async Task<ApiResponse> GetCardNumber(int coachId)
        {
            var cardNumber = await context.CoachCardNumbers.Where(c => c.CoachId == coachId).FirstOrDefaultAsync();
            if (cardNumber is null)
            {
                return new ApiResponse
                {
                    Action = true, Message = "اطلاعاتی موجود نیست"
                };
            }


            return new ApiResponse
            {
                Action = true, Message = "اطلاعات پیدا شد",
                Result = cardNumber
            };
        }

        #endregion


        public async Task<ApiResponse> GetProfile(string phoneNumber)
        {
            var user = await context.Users
                .Include(u => u.Coach)
                .ThenInclude(c => c.CoachingServices)
                .Include(c=>c.Coach.AthleteChangePhotos)
                .Include(u=>u.Coach.WorkoutProgramFeedbacks)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user?.Coach == null)
                return new ApiResponse { Action = false, Message = "Coach not found" };

            var coachingService = user.Coach.CoachingServices
                .Where(x => !x.IsDeleted)
                .ToList();

            var coachingServiceDto = coachingService
                .Select(x => x.ToCoachingServiceResponse())
                .ToList();

            var numberOfProgram = await context.Payments
                .Where(p =>
                    p.CoachId == user.Coach.Id &&
                    p.PaymentStatus == PaymentStatus.SUCCESS &&
                    p.WorkoutProgram != null &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.WRITING &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.NOTSTARTED &&
                    p.WorkoutProgram.Status != WorkoutProgramStatus.UNCOMPLETEDQUESTION)
                .CountAsync();

            var numberOfAthlete = await context.Payments
                .Where(p =>
                    p.CoachId == user.Coach.Id &&
                    p.PaymentStatus == PaymentStatus.SUCCESS)
                .Select(p => p.AthleteId)
                .Distinct()
                .CountAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Coach found",
                Result = user.ToCoachProfileResponseDto(
                    coachingServiceDto,
                    numberOfProgram,
                    numberOfAthlete
                )
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
                await context.SaveChangesAsync();
                
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

                    if (workoutProgram.ProgramInDays.Count == 0)
                    {
                        return new ApiResponse()
                        {
                            Action = false,
                            Message = "list is empty"
                        };
                    }
                   

                    coach.Amount += (workoutProgram.Payment.Amount - workoutProgram.Payment.AppFee);              
                    var athleteImg = await context.AthleteImage
                        .Where(a => a.AthleteQuestionId == workoutProgram.Payment.AthleteQuestionId)
                        .FirstOrDefaultAsync();
                    if (athleteImg?.SideLink != null)
                    {
                            await storage.RemovePhoto(athleteImg.SideLink);
                    }
                    if (athleteImg?.BackLink != null)
                    {
                        await storage.RemovePhoto(athleteImg.BackLink);
                    }
                    if (athleteImg?.FrontLink != null)
                    {
                        await storage.RemovePhoto(athleteImg.FrontLink);
                    }

                    if (athleteImg is not null)
                    {
                         context.AthleteImage.Remove(athleteImg);
                    }
                    

                    await smsService.WorkoutReadySms(athlete.PhoneNumber, athlete.User.FirstName, workoutProgram.Title,token.HashEncode(workoutProgram.Id));
                    var exerciseIds = workoutProgramDto.Days
                        .SelectMany(d => d.AllExerciseInDays)
                        .Select(ex => ex.ExerciseId).ToList();
                    var workoutExercises = new LastWorkoutExercise()
                    {
                        CoachId = coach.Id,
                        AthleteId = athlete.Id,
                        WorkoutProgramId = workoutProgram.Id,
                        ExerciseIds = exerciseIds

                    }; 
                    await context.LastWorkoutExercises.AddAsync(workoutExercises);
                    await context.SaveChangesAsync();
                    
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
                        var index = (day - 1) % programInDayCount;
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
                {
                    CoachAmount = coachAmount,
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
            var coachCartNumber = await context.CoachCardNumbers.Where(c => c.CoachId == coach.Id).AnyAsync();

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
                    // PublicDiscountAmount = p.PublicDiscountAmount,
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
                    coachPayoutDto,
                    CardNumberIsSet = coachCartNumber
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

     public async Task<(IEnumerable<AllExerciseResponseDto>, int)>
GetExercisesWithFilterForCoach(
    string? level,
    string? type,
    string? mechanic,
    string?[]? equipment,
    string? muscle,
    string? place,
    int page,
    int pageSize,
    string? searchTerm,
    int? athleteId,
    int coachId)
{
    if (page < 1) page = 1;
    if (pageSize <= 0) pageSize = 10;

    var exercises = await exerciseCache.GetAllExercisesAsync();
    var pins = await exerciseCache.GetCoachPinsAsync(coachId);
    var lastWorkouts = await exerciseCache.GetCoachLastWorkoutsAsync(coachId);

    IEnumerable<Exercise> query = exercises;

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(e =>
            e.PersianName.Contains(searchTerm) ||
            e.EnglishName.Contains(searchTerm));
    }

    if (Enum.TryParse(level, true, out ExerciseLevel levelEnum))
        query = query.Where(e => e.ExerciseLevel == levelEnum);

    if (Enum.TryParse(type, true, out ExerciseType typeEnum))
        query = query.Where(e => e.ExerciseType == typeEnum);

    if (Enum.TryParse(mechanic, true, out MechanicType mechanicEnum))
        query = query.Where(e => e.Mechanics == mechanicEnum);

    if (Enum.TryParse(muscle, true, out BaseCategory muscleEnum))
        query = query.Where(e => e.BaseCategory == muscleEnum);

    var pinnedIds = pins
        .SelectMany(x => x.Value)
        .ToHashSet();

    var lastIds = athleteId.HasValue &&
                  lastWorkouts.ContainsKey(athleteId.Value)
        ? lastWorkouts[athleteId.Value]
        : new List<int>();

    var total = query.Count();

    var result = query
        .Select(e => new
        {
            Exercise = e,
            IsPinned = pinnedIds.Contains(e.Id),
            IsLast = lastIds.Contains(e.Id)
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
            IsInLastProgram = x.IsLast
        });

    return (result, total);
}



public async Task<ApiResponse> AddPineExercise(int exerciseId, int coachId)
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

    var exercises = await exerciseCache.GetAllExercisesAsync();

    var exercise = exercises.FirstOrDefault(e => e.Id == exerciseId);

    if (exercise == null)
    {
        return new ApiResponse
        {
            Message = "exercise not found",
            Action = false
        };
    }

    var oldPineExercises = await context.CoachPineExercises
        .FirstOrDefaultAsync(ex =>
            ex.CoachId == coach.Id &&
            ex.BaseCategory == exercise.BaseCategory);

    if (oldPineExercises == null)
    {
        var newCategory = new CoachPineExercise
        {
            CoachId = coach.Id,
            BaseCategory = exercise.BaseCategory,
            ExerciseIds = new List<int> { exerciseId }
        };

        await context.CoachPineExercises.AddAsync(newCategory);
    }
    else
    {
        if (!oldPineExercises.ExerciseIds.Contains(exerciseId))
        {
            oldPineExercises.ExerciseIds.Add(exerciseId);
        }
    }

    await context.SaveChangesAsync();

    await exerciseCache.UpdateCoachPinsAsync(
        coachId,
        exercise.BaseCategory,
        exerciseId);

    return new ApiResponse
    {
        Message = "added",
        Action = true
    };
}

        public async Task<ApiResponse> RemovePineExercise(int exerciseId, int coachId)
        {
            var coach = await context.Coaches
                .FirstOrDefaultAsync(c => c.Id == coachId);

            if (coach == null)
                return new ApiResponse
                {
                    Message = "coach not found",
                    Action = false
                };

            var pins = await context.CoachPineExercises
                .Where(ex => ex.CoachId == coach.Id)
                .ToListAsync();

            var target = pins
                .FirstOrDefault(ex => ex.ExerciseIds.Contains(exerciseId));

            if (target == null)
                return new ApiResponse
                {
                    Message = "exercise not found",
                    Action = false
                };

            target.ExerciseIds.Remove(exerciseId);

            await context.SaveChangesAsync();

            await exerciseCache.RemoveCoachPinAsync(coachId, exerciseId);

            return new ApiResponse
            {
                Message = "remove pine",
                Action = true
            };
        }
        public async Task<ApiResponse> GetCoachChecklist(string phoneNumber)
        {
            var user = await context.Users
                .Include(u => u.Coach)
                .ThenInclude(c => c.AthleteChangePhotos)
                .Include(u => u.Coach)
                .ThenInclude(c => c.WorkoutProgramFeedbacks)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (user.Coach == null)
                return new ApiResponse
                {
                    Message = "coach not found",
                    Action = false
                };
            var hasPersonalDetails = !string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(user.ImageProfile);
    
            var hasCommunication = !string.IsNullOrWhiteSpace(user.Coach.InstagramLink) || !string.IsNullOrWhiteSpace(user.Coach.TelegramLink) || !string.IsNullOrWhiteSpace(user.Coach.WhatsApp) || !string.IsNullOrWhiteSpace(user.Coach.BaleUserName) || !string.IsNullOrWhiteSpace(user.Coach.EitaaUserName);
    
            var hasWebsite = !string.IsNullOrWhiteSpace(user.Coach.WebSiteUrl);
    
            var hasAthleteChange = user.Coach.AthleteChangePhotos?.Any() == true;
    
            var hasReviews = user.Coach.WorkoutProgramFeedbacks?.Any() == true;

      

            var response = new ProfileChecklistResponse
            {
                HasPersonalDetails = hasPersonalDetails,
                HasCommunicationChannels = hasCommunication,
                HasWebsiteAddress = hasWebsite,
                HasAthleteChange = hasAthleteChange,
                HasUserReviews = hasReviews,
            };
            return new ApiResponse()
            {
                Action = true,
                Message = "data",
                Result = response
            };
        }



        public async Task<ApiResponse> GetWorkoutProgramFeedBack(string phoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null)
            {
                return new ApiResponse { Action = false, Message = "مربی یافت نشد." };
            }

            var feedBack = await context.WorkoutProgramFeedback.Where(fb => fb.CouchId == coach.Id).ToListAsync();
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

        // private ApiResponse ValidatePublicDiscount(AddCoachServiceDto dto)
        // {
        //     if (dto.PublicDiscountPercent is null || dto.PublicDiscountPercent <= 0)
        //     {
        //         dto.UsageLimit = null;
        //         dto.PublicDiscountExpiresAt = null;
        //         dto.PublicDiscountPercent = null;
        //         return new ApiResponse
        //         {
        //             Action = true ,
        //             Message = "کد تخفیفی ایجاد نشده است"
        //         };
        //     }
        //     var now = DateTime.Now;
        //
        //  
        //
        //     if (dto.PublicDiscountExpiresAt.HasValue && dto.PublicDiscountExpiresAt.HasValue &&
        //         now >= dto.PublicDiscountExpiresAt.Value)
        //     {
        //         return new ApiResponse { Action = false, Message = "تاریخ انقضا قبل از تاریخ شروع است" };
        //     }
        //
        //     var amount = CoachMappers.CalculateDiscountAmount(dto.Price, dto.PublicDiscountPercent.Value);
        //     if (amount <= 0)
        //     {
        //         return new ApiResponse { Action = false, Message = "مقدار تخفیف عمومی نامعتبر است." };
        //     }
        //
        //     return new ApiResponse
        //     {
        //         Action = true
        //         , Message = "کد تخفیف درست است"
        //     };
        // }

        private async Task<ApiResponse> ValidateDiscountCodeInput(Coach coach, string code, double DiscountPercent,
            int? usageLimit, DateTime? expiresAt, int? currentDiscountCodeId, List<int>? coachServiceIdsDto,bool appliesToAllServices)
        {
            var now = DateTime.Now;

            if (DiscountPercent <= 0)
            {
                return new ApiResponse { Action = false, Message = "مقدار تخفیف باید بیشتر از صفر باشد." };
            }

            if (usageLimit is <= 0)
            {
                return new ApiResponse { Action = false, Message = "سقف استفاده باید بیشتر از صفر باشد." };
            }

            if (!appliesToAllServices&&coachServiceIdsDto is not null && coachServiceIdsDto.Count != 0)
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


            if (expiresAt.HasValue && now > expiresAt.Value)
            {
                return new ApiResponse { Action = false, Message = "تاریخ شروع باید قبل از تاریخ انقضا باشد." };
            }

            var exists = await context.DiscountCodes
                .AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentDiscountCodeId);

            if (exists)
            {
                return new ApiResponse { Action = false, Message = "این کد تخفیف قبلاً ثبت شده است." };
            }

            return new ApiResponse
            {
                Action = true,
                Message = "کد تخفیف درست است ",
                Result = code
            };
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