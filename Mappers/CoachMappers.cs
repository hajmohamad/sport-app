using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Coach;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.TrainingPlan;
using static sport_app_backend.Models.Account.Coach.CoachStatus;

namespace sport_app_backend.Mappers
{
    public static class CoachMappers
    {
        private static CoachProfileStatus GetCoachProfileStatus(
            CoachStatus currentStatus,
            bool isFullyCompleted)
        {
            if (currentStatus >= CoachStatus.HiddenInList)
                return CoachProfileStatus.Verified;

            if (isFullyCompleted)
                return CoachProfileStatus.PendingApproval;

            return CoachProfileStatus.NeedsCompletion;
        }


        public static CoachProfileResponse ToCoachProfileResponseDto(
            this User user,
            List<CoachingServiceResponse> coachingServicesResponse,
            int numberOfProgram,
            int numberOfAthlete)
        {
            var completion = ValidateCoachProfile(user, coachingServicesResponse);
            var status = GetCoachProfileStatus(
                user.Coach?.CoachStatus ?? None,
                completion.IsFullyCompleted);

            const string baseUrl = "https://chaarset.ir/coach/";
            var websiteUrl = !string.IsNullOrEmpty(user.Coach?.WebSiteUrl)
                ? $"{baseUrl}{user.Coach.WebSiteUrl}/"
                : null;

            return new CoachProfileResponse
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName ?? string.Empty,
                Id = user.Id,
                Gender = user.Gender.ToString(),
                ImageProfile = user.ImageProfile,
                CoachingServices = coachingServicesResponse,
                NumberOfAthlete = numberOfAthlete,
                NumberOfProgram = numberOfProgram,
                SiteDescription = user.Coach?.SiteDescription,
                Slogan = user.Coach?.Slogan,
                HasPersonalDetails = completion.HasPersonalDetails,
                HasCommunicationChannels = completion.HasCommunication,
                HasWebsiteAddress = completion.HasWebsite,
                HasUserReviews = completion.HasReviews,
                HasAthleteChange = completion.HasAthleteChange,
                CompletionPercentage = completion.CompletionPercentage,
                ShowWebsite = status >= CoachProfileStatus.PendingApproval,
                Status = status,
                WebsiteStatus = BuildWebSiteDto(status, websiteUrl)
            };
        }


        public static ProfileCompletionResult ValidateCoachProfile(
            User user,
            List<CoachingServiceResponse> coachingServicesResponse)
        {
            var coach = user.Coach;

            var result = new ProfileCompletionResult
            {
                HasPersonalDetails =
                    !string.IsNullOrWhiteSpace(user.FirstName) &&
                    !string.IsNullOrWhiteSpace(user.LastName) &&
                    !string.IsNullOrWhiteSpace(user.ImageProfile) &&
                    !string.IsNullOrWhiteSpace(coach?.Slogan) &&
                    !string.IsNullOrWhiteSpace(coach?.SiteDescription),

                HasCommunication = coach != null && (
                    !string.IsNullOrWhiteSpace(coach.InstagramLink) ||
                    !string.IsNullOrWhiteSpace(coach.TelegramLink) ||
                    !string.IsNullOrWhiteSpace(coach.WhatsApp) ||
                    !string.IsNullOrWhiteSpace(coach.BaleUserName) ||
                    !string.IsNullOrWhiteSpace(coach.EitaaUserName)),

                HasCoachingService = coachingServicesResponse.Count != 0,

                HasWebsite = !string.IsNullOrWhiteSpace(coach?.WebSiteUrl),

                HasReviews = coach?.WorkoutProgramFeedbacks.Any(wf => wf.IsChosen) ?? false,

                HasAthleteChange = coach is { AthleteChangePhotos.Count: > 0 }
            };

            result.CompletionPercentage = 0;
            if (result.HasPersonalDetails) result.CompletionPercentage += 20;
            if (result.HasCommunication) result.CompletionPercentage += 20;
            if (result.HasCoachingService) result.CompletionPercentage += 25;
            if (result.HasWebsite) result.CompletionPercentage += 15;
            if (result.HasReviews) result.CompletionPercentage += 10;
            if (result.HasAthleteChange) result.CompletionPercentage += 10;

            return result;
        }


        private static WebsiteStatus BuildWebSiteDto(CoachProfileStatus status, string? coachWebSiteUrl)
        {
            switch (status)
            {
                case CoachProfileStatus.NeedsCompletion:
                    return new WebsiteStatus
                    {
                        WebSiteMessage = "برای فعال شدن وبسایت ، بخش ها مورد نیاز را از صفحه تنظیمات، وبسایت من،‌ تکمیل کنید",
                        WebsiteUrl = null,
                        ShowShareWebsite = false
                    };

                case CoachProfileStatus.PendingApproval:
                    return new WebsiteStatus
                    {
                        WebSiteMessage = "وب‌سایت شما به‌زودی توسط تیم چارست تأیید می‌شود",
                        WebsiteUrl = coachWebSiteUrl,
                        ShowShareWebsite = false
                    };

                case CoachProfileStatus.Verified:
                default:
                    return new WebsiteStatus
                    {
                        WebSiteMessage = "وب‌سایت شما آماده است",
                        WebsiteUrl = coachWebSiteUrl,
                        ShowShareWebsite = true
                    };
                
            }

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
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageProfile = user.ImageProfile,
             
            };

        }

 
        public static CoachProfileForAthleteDto ToCoachProfileForAthleteDto(this Coach coach,int numberOfProgram,int numberOfAthlete)
        {
            return new CoachProfileForAthleteDto
            {
                FirstName = coach.User.FirstName,
                LastName = coach.User.LastName,
                UserName = coach.User.UserName ?? string.Empty,
                Id = coach.Id,
                ImageProfile = coach.User.ImageProfile,
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
        //     var now = DateTime.Now;
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

            if (discountCode.UsageLimit<=discountCode.UsedCount||(discountCode.ExpiresAt.HasValue && discountCode.ExpiresAt.Value <= DateTime.Now))
            {
                return DiscountCodeStatus.EXPIRED;
            }

            return DiscountCodeStatus.ACTIVE;
        }

        public static CoachDecreaseDto ToCoachPayoutDto(this CoachPayout coachPayout)
        {
            return new CoachDecreaseDto()
            {
                Amount = coachPayout.Amount,
                RequestDate = coachPayout.RequestDate.ToString(CultureInfo.InvariantCulture),
                Status = coachPayout.Status.ToString(),
                PaidDate = coachPayout.PaidDate.ToString() ?? "",
                TransactionReference = coachPayout.TransactionReference??"",
                IsDirectProgram = false,
                Descriptions = "تسویه"
                
            };
        }
        public static CoachDecreaseDto ToDirectProgramDto(this Payment payment)
        {
            var athleteName = $"{payment.Athlete.User.FirstName} {payment.Athlete.User.LastName}";
            return new CoachDecreaseDto()
            {
                Amount = payment.AppFee,
                RequestDate = payment.PaymentDate.ToString(CultureInfo.InvariantCulture),
                Status = "Paid",
                PaidDate = payment.PaymentDate.ToString(CultureInfo.InvariantCulture) ,
                TransactionReference = payment.RefId.ToString(),
                Descriptions = $"طراحی برنامه ورزشی برای {athleteName}",
                IsDirectProgram = true,
            };
        }
        

  

    }

}
