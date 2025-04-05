using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Mappers
{
    public static class CoachMappers
    {
        public static CoachProfileResponse ToCoachProfileResponseDto(this User user)
        {   
            
            return new Dtos.CoachProfileResponse
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
                PhoneNumber = user.PhoneNumber,
                UserName= user.UserName ?? string.Empty,
                Id = user.Id,
                Gender = user.Gender.ToString(),
                ImageProfile = user.ImageProfile ?? Array.Empty<byte>(),
                Bio = user.Bio ?? [],
                Domain = user.Coach?.Domain?.Select(x => x.ToString()).ToList() ?? [], // Ensure it's not null
                StartCoachingYear = user.Coach?.StartCoachingYear ?? default
            };



        }
        public static CoachPlan ToCoachPlane(this AddCoachingPlaneDto coachPlaneDto,Coach coach)
        {
            return new CoachPlan{
                Coach = coach,
                CoachId = coach.Id,
                Title = coachPlaneDto.Title,
                Description = coachPlaneDto.Description,
                Price = coachPlaneDto.Price,
                IsActive = coachPlaneDto.IsActive,
                HaveSupport = coachPlaneDto.HaveSupport,
                CommunicateType = coachPlaneDto.CommunicateType,
                TypeOfCoachingPlan = (TypeOfCoachingPlan)Enum.Parse(typeof(TypeOfCoachingPlan), coachPlaneDto.TypeOfCoachingPlan)
            };
        }

        public static CoachPlan UpdateCoachingPlane(this CoachPlan coachingPlane,AddCoachingPlaneDto coachPlaneDto)
        {
            coachingPlane.Title = coachPlaneDto.Title;
            coachingPlane.Description = coachPlaneDto.Description;
            coachingPlane.Price = coachPlaneDto.Price;
            coachingPlane.IsActive = coachPlaneDto.IsActive;
            coachingPlane.HaveSupport = coachPlaneDto.HaveSupport;
            coachingPlane.CommunicateType = coachPlaneDto.CommunicateType;
            coachingPlane.TypeOfCoachingPlan =
                (TypeOfCoachingPlan)Enum.Parse(typeof(TypeOfCoachingPlan), coachPlaneDto.TypeOfCoachingPlan);
            return coachingPlane;

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
                UserName = user.UserName,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                ImageProfile = user.ImageProfile ?? Array.Empty<byte>(),
                Bio = user.Bio ?? []
            };

        }

 
        public static CoachProfileForAthleteDto ToCoachProfileForAthleteDto(this Coach coach)
        {
            return new CoachProfileForAthleteDto
            {
                FirstName = coach.User?.FirstName ?? string.Empty,
                LastName = coach.User?.LastName ?? string.Empty,
                UserName = coach.User?.UserName ?? string.Empty,
                Id = coach.Id,
                ImageProfile = coach.User?.ImageProfile ?? Array.Empty<byte>(),
                Bio = coach.User?.Bio ?? [],
                Domain = coach.Domain?.Select(x => x.ToString()).ToList() ?? new List<string>(), // Ensure it's not null
                StartCoachingYear = coach.StartCoachingYear,
                Coachplans = coach.CoachingPlans.Where(x=>!x.IsDeleted)?.Select(x => x.ToCoachingPlanResponse()).ToList() ?? new List<CoachingPlanResponse>()
            };
        }




        public static CoachingPlanResponse ToCoachingPlanResponse(this CoachPlan coachPlan)
        {
            return new CoachingPlanResponse
            {
                Id = coachPlan.Id,
                Title = coachPlan.Title,
                Description = coachPlan.Description,
                Price = coachPlan.Price,
                IsActive = coachPlan.IsActive,
                HaveSupport = coachPlan.HaveSupport,
                CommunicateType = coachPlan.CommunicateType,
                TypeOfCoachingPlan = coachPlan.TypeOfCoachingPlan.ToString()
            };
        }

  

    }

}