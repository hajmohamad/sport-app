using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Dtos
{
    public class CoachProfileForAthleteDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public required string UserName { get; set; }

        public byte[] ImageProfile { get; set; } = Array.Empty<byte>();
        public string Bio { get; set; } = string.Empty;

        public int CoachId { get; set; }


        public List<string>? Domain { get; set; }

        public int StartCoachingYear { get; set; }

        public List<CoachingPlanResponse> Coachplans { get; set; } = [];

    }
}