using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Mappers
{
    public static class AthleteMappers
    {
        public static AthleteProfileResponse? ToAthleteProfileResponseDto(this User user)
        {
            if (user.Athlete == null) return null;
            return new AthleteProfileResponse
            {
                FirstName = user.FirstName?? string.Empty,
                LastName = user.LastName??string.Empty,
                UserName = user.UserName,
                BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
                PhoneNumber = user.PhoneNumber,
                Id = user.Id,
                Height = user.Athlete.Height,
                CurrentWeight = user.Athlete.CurrentWeight,
                WeightGoal = user.Athlete.WeightGoal,
                InjuryArea = user.Athlete.InjuryArea.Select(x => x.ToString()).ToList(),
                FitnessLevel = user.Athlete.FitnessLevel.ToString(),
                CurrentBodyForm = user.Athlete.CurrentBodyForm,
                TargetBodyForm = user.Athlete.TargetBodyForm,
                Gender = user.Gender.ToString(),
                ImageProfile = user.ImageProfile ?? Array.Empty<byte>(),
                Bio = user.Bio ?? string.Empty};
                }
       
    }
}