using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;
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
                WebsiteUrl = user.Coach.WebSiteUrl??""
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
                HaveSupport = coachServiceDto.HaveSupport,
                // CommunicateType = (CommunicateType)Enum.Parse(typeof(CommunicateType), coachServiceDto.CommunicateType.ToUpper()),
                NumberOfSell = 0
                // TypeOfCoachingServices = (TypeOfCoachingServices)Enum.Parse(typeof(TypeOfCoachingServices), coachServiceDto.TypeOfCoachingServices)
            };
        }

        public static void UpdateCoachServices(this CoachService coachService,AddCoachServiceDto coachServiceDto)
        {
            coachService.Title = coachServiceDto.Title;
            coachService.Description = coachServiceDto.Description;
            coachService.Price = coachServiceDto.Price;
            coachService.IsActive = coachServiceDto.IsActive;
            coachService.HaveSupport = coachServiceDto.HaveSupport;
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
                WhatsApp = coach.WhatsApp
            };
        }




        public static CoachingServiceResponse ToCoachingServiceResponse(this CoachService coachService)
        {
            return new CoachingServiceResponse
            {
                Id = coachService.Id,
                Title = coachService.Title,
                Description = coachService.Description,
                Price = coachService.Price,
                IsActive = coachService.IsActive,
                HaveSupport = coachService.HaveSupport,
                // CommunicateType = coachService.CommunicateType.ToString(),
                NumberOfSell = coachService.NumberOfSell
                
                
            };
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