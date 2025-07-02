using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Payments;

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
                Bio = user.Bio,
                HeadLine = user.Coach?.HeadLine?? string.Empty,
                Domain = user.Coach?.Domain?.Select(x => x.ToString())
                             .ToList() ??
                         [], // Ensure it's not null
                StartCoachingYear = user.Coach?.StartCoachingYear ?? 0,
                CoachingServices = coachingServicesResponse,
                Payments = payments.Select(p => p.ToCoachAllPaymentResponseDto())
                    .ToList(),
                NumberOfAthlete = numberOfAthlete,
                NumberOfProgram = numberOfProgram
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
                CommunicateType = coachServiceDto.CommunicateType??"",
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
            coachService.CommunicateType = coachServiceDto.CommunicateType??"";
            //coachService.TypeOfCoachingServices =
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
                Bio = user.Bio,
                HeadLine = user.Coach?.HeadLine?? string.Empty,

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
                Bio = coach.User?.Bio ?? "",
                HeadLine = coach.HeadLine,
                Domain = coach.Domain?.Select(x => x.ToString())
                             .ToList() ??
                         new List<string>(), // Ensure it's not null
                StartCoachingYear = coach.StartCoachingYear,
                CoachServices = coach.CoachingServices.Where(x => !x.IsDeleted)
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
                CommunicateType = coachService.CommunicateType,
                NumberOfSell = coachService.NumberOfSell
                
                
            };
        }

  

    }

}