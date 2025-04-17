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

        public string ImageProfile { get; set; }="";
        public string Bio { get; set; } = "";
        public string HeadLine { get; set; } = "";


        public int CoachId { get; set; }


        public List<string>? Domain { get; set; }

        public int StartCoachingYear { get; set; }

        public List<CoachingServiceResponse> CoachServices { get; set; } = [];
        public required int NumberOfAthletes { get; set; }
        public required int NumberOfProgram { get; set; }

    }
}