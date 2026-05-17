using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Mappers
{
    public static class CoachMappers
    {
        public static CoachProfileResponse ToCoachProfileResponseDto(this User user,List<CoachingServiceResponse> coachingServicesResponse
            ,List<Payment> payments)
        {   
             var numberOfProgram = payments.Count(p => p.WorkoutProgram != null);
              var numberOfAthlete = payments.Select(x=>x.AthleteId).Distinct().Count();
            
            return new CoachProfileResponse
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName ?? string.Empty,
                Id = user.Id,
                Gender = user.Gender.ToString(),
                ImageProfile = user.ImageProfile,
                CoachingServices = coachingServicesResponse,
                Payments = payments.Select(p => p.ToCoachAllPaymentResponseDto())
                    .ToList(),
                NumberOfAthlete = numberOfAthlete,
                NumberOfProgram = numberOfProgram,
                WebsiteUrl = user.Coach?.WebSiteUrl ?? null
            };



        }

      
        public static CoachService ToCoachService(this AddCoachServiceDto coachServiceDto,Coach coach)
        {
            return new CoachService{
                Coach = coach,
                CoachId = coach.Id,
                Title = coachServiceDto.Title,
                Description = coachServiceDto.Description,
                Price = coachServiceDto.Price,
                IsActive = coachServiceDto.IsActive, 
                // PublicDiscountPercent = coachServiceDto.PublicDiscountPercent,
                // PublicDiscountExpiresAt = coachServiceDto.PublicDiscountExpiresAt,
                // UsageLimit = coachServiceDto.UsageLimit,
                // NumberOfSellWithDiscount = 0,
                NumberOfSell = 0
                // CommunicateType = (CommunicateType)Enum.Parse(typeof(CommunicateType), coachServiceDto.CommunicateType.ToUpper()),
                // TypeOfCoachingServices = (TypeOfCoachingServices)Enum.Parse(typeof(TypeOfCoachingServices), coachServiceDto.TypeOfCoachingServices)
            };
        }

        public static void UpdateCoachServices(this CoachService coachService,AddCoachServiceDto coachServiceDto)
        {
            coachService.Title = coachServiceDto.Title;
            coachService.Description = coachServiceDto.Description;
            coachService.Price = coachServiceDto.Price;
            coachService.IsActive = coachServiceDto.IsActive;
            // coachService.PublicDiscountPercent = coachServiceDto.PublicDiscountPercent;
            // coachService.PublicDiscountExpiresAt = coachServiceDto.PublicDiscountExpiresAt;
            // coachService.PublicDiscountPercent = coachServiceDto.PublicDiscountPercent;
            // coachService.PublicDiscountExpiresAt = coachServiceDto.PublicDiscountExpiresAt;
            // coachService.UsageLimit = coachServiceDto.UsageLimit;
            
            // coachService.CommunicateType =
            //     (CommunicateType)Enum.Parse(typeof(CommunicateType), coachServiceDto.CommunicateType.ToUpper());
            // //coachService.TypeOfCoachingServices =
            //(TypeOfCoachingServices)Enum.Parse(typeof(TypeOfCoachingServices), coachServiceDto.TypeOfCoachingServices);


        }
        public static CoachForSearch ToCoachForSearch(this User user)
        {
            if (user.Coach == null) return new CoachForSearch()
            {
                UserName = string.Empty
            };
            return new CoachForSearch
            {
                Id = user.Coach.Id,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                ImageProfile = user.ImageProfile,
             
            };

        }

 
        public static CoachProfileForAthleteDto ToCoachProfileForAthleteDto(this Coach coach,int numberOfProgram,int numberOfAthlete)
        {
            return new CoachProfileForAthleteDto
            {
                FirstName = coach.User?.FirstName ?? string.Empty,
                LastName = coach.User?.LastName ?? string.Empty,
                UserName = coach.User?.UserName ?? string.Empty,
                Id = coach.Id,
                ImageProfile = coach.User?.ImageProfile ?? "",
                CoachServices = coach.CoachingServices.Where(x => x is { IsDeleted: false, IsActive: true })
                                    .Select(x => x.ToCoachingServiceResponse())
                                    .ToList() ,
                NumberOfAthletes = numberOfAthlete,
                NumberOfProgram = numberOfProgram,
                InstagramLink = coach.InstagramLink,
                TelegramLink = coach.TelegramLink,
                WhatsApp = coach.WhatsApp,
                BaleUserName = coach.BaleUserName,
                EitaaUserName = coach.EitaaUserName
            };
        }




        public static CoachingServiceResponse ToCoachingServiceResponse(this CoachService coachService)
        {
            // var publicDiscountAmount = CalculatePublicDiscountAmount(coachService);
            return new CoachingServiceResponse
            {
                Id = coachService.Id,
                Title = coachService.Title,
                Description = coachService.Description,
                Price = coachService.Price,
                // FinalPrice = Math.Max(0, coachService.Price - publicDiscountAmount),
                IsActive = coachService.IsActive,
                // HasPublicDiscount = publicDiscountAmount > 0,
                // PublicDiscountPercent = coachService.PublicDiscountPercent,
                // PublicDiscountExpiresAt = coachService.PublicDiscountExpiresAt,
                NumberOfSell = coachService.NumberOfSell
                
                
            };
        }

        public static DiscountCodeListItemDto ToDiscountCodeListItemDto(this DiscountCode discountCode,List<ServiceForDiscountDto>? serviceForDiscountDto)
        {
            var status = discountCode.GetEffectiveStatus();
            return new DiscountCodeListItemDto
            {
                Id = discountCode.Id,
                Code = discountCode.Code,
                DiscountPercent = discountCode.DiscountPercent,
                UsageLimit = discountCode.UsageLimit,
                UsedCount = discountCode.UsedCount,
                ExpiresAt = discountCode.ExpiresAt,
                Status = status.ToString(),
                ServiceForDiscountDtos = serviceForDiscountDto
            };
        }

        // public static double CalculatePublicDiscountAmount(this CoachService coachService)
        // {
        //     if (coachService.PublicDiscountPercent is null)
        //     {
        //         return 0;
        //     }
        //
        //     var now = DateTime.UtcNow;
        //  
        //
        //     if (coachService.PublicDiscountExpiresAt.HasValue && coachService.PublicDiscountExpiresAt.Value <= now)
        //     {
        //         return 0;
        //     }
        //
        //     return CalculateDiscountAmount(coachService.Price,
        //         coachService.PublicDiscountPercent.Value);
        // }

        public static double CalculateDiscountAmount(double basePrice,double discountPercent)
        {
            if (basePrice <= 0 || discountPercent <= 0)
            {
                return 0;
            }

            var amount =
                basePrice * (discountPercent / 100d);

            return amount;
        }


        private static DiscountCodeStatus GetEffectiveStatus(this DiscountCode discountCode)
        {
            if (discountCode.Status == DiscountCodeStatus.INACTIVE)
            {
                return DiscountCodeStatus.INACTIVE;
            }

            if (discountCode.UsageLimit<=discountCode.UsedCount||(discountCode.ExpiresAt.HasValue && discountCode.ExpiresAt.Value <= DateTime.UtcNow))
            {
                return DiscountCodeStatus.EXPIRED;
            }

            return DiscountCodeStatus.ACTIVE;
        }

        public static CoachPayoutDto ToCoachPayoutDto(this CoachPayout coachPayout)
        {
            return new CoachPayoutDto()
            {
                Amount = coachPayout.Amount,
                RequestDate = coachPayout.RequestDate.ToString(CultureInfo.InvariantCulture),
                Status = coachPayout.Status.ToString(),
                PaidDate = coachPayout.PaidDate.ToString() ?? "",
                TransactionReference = coachPayout.TransactionReference??""
            };
        }

  

    }

}
