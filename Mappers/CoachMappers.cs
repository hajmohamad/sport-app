using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Coach;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Mappers
{
    public static class CoachMappers
    {
        public static CoachProfileResponse ToCoachProfileResponseDto(
    this User user,
    List<CoachingServiceResponse> coachingServicesResponse,
    int numberOfProgram,
    int numberOfAthlete)
{
    if(user.Coach is null);
    const string baseUrl = "https://chaarset.ir/coach/";
    var websiteUrl = !string.IsNullOrEmpty(user.Coach?.WebSiteUrl)
        ? $"{baseUrl}{user.Coach.WebSiteUrl}/"
        : null;

    var hasPersonalDetails =
        !string.IsNullOrWhiteSpace(user.FirstName) &&
        !string.IsNullOrWhiteSpace(user.LastName) &&
        !string.IsNullOrWhiteSpace(user.ImageProfile);

    var hasCommunication = user.Coach != null && (
        !string.IsNullOrWhiteSpace(user.Coach.InstagramLink) ||
        !string.IsNullOrWhiteSpace(user.Coach.TelegramLink) ||
        !string.IsNullOrWhiteSpace(user.Coach.WhatsApp) ||
        !string.IsNullOrWhiteSpace(user.Coach.BaleUserName) ||
        !string.IsNullOrWhiteSpace(user.Coach.EitaaUserName)
    );

    var hasCoachingService = user.Coach != null && user.Coach.CoachingServices.Count != 0;

    var hasWebsite = !string.IsNullOrWhiteSpace(user.Coach?.WebSiteUrl);

    var hasReviews = user.Coach?.WorkoutProgramFeedbacks.Count != 0;

    var showWebSite = user.Coach?.ShowWebsite ?? false;
    var hasAthleteChange = user.Coach !=null && user.Coach.AthleteChangePhotos.Count != 0;
    var isVerified =user.Coach is { Verified: true };

    int completionPercentage = 0;
    if (hasPersonalDetails) completionPercentage += 20;
    if (hasCommunication) completionPercentage += 20;
    if (hasCoachingService) completionPercentage += 25;
    if (hasWebsite) completionPercentage += 15;
    if (hasReviews) completionPercentage += 10;
    if (hasAthleteChange) completionPercentage += 10;

    
    

    bool needsCompletion = !hasCommunication || !hasWebsite||!hasCoachingService;
    bool pendingApproval = hasCommunication && hasWebsite && !showWebSite;

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
        NumberOfAthlete = numberOfAthlete,
        NumberOfProgram = numberOfProgram,

        WebsiteUrl = websiteUrl,
        SiteDescription = user.Coach?.SiteDescription,
        Slogan = user.Coach?.Slogan,

        HasPersonalDetails = hasPersonalDetails,
        HasCommunicationChannels = hasCommunication,
        HasWebsiteAddress = hasWebsite,
        HasUserReviews = hasReviews,
        HasAthleteChange=hasAthleteChange,
        CompletionPercentage = completionPercentage,

        NeedsCompletion = needsCompletion,
        PendingApproval = pendingApproval,
        IsVerified = isVerified
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
                ServiceForDiscountDtos = serviceForDiscountDto,
                AppliesToAllServices = discountCode.AppliesToAllServices
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
