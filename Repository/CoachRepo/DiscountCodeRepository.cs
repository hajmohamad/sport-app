using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Payments;

namespace sport_app_backend.Repository.CoachRepo;

public class DiscountCodeRepository(
    ApplicationDbContext context,
    ISmsService smsService,
    IStorage storage,
    ITokenService token,
    ICalculator calculator,
    IExerciseCacheService exerciseCache,
    IZarinPal zarinPal
) :IDiscountCodeRepository
{


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

}