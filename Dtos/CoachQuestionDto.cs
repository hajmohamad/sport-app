using sport_app_backend.Models.Account;
using sport_app_backend.Models.C_Question;

namespace sport_app_backend.Dtos
{
    public class CoachQuestionDto
    {
        public List<string> Disciplines { get; set; } = [];
        public List<string> Motivations { get; set; } = [];
        public bool WorkOnlineWithAthletes { get; set; }
        public List<string> PresentsPracticeProgram { get; set; } = [];
        public string TrackAthlete { get; set; } = "";
        public bool ManagingRevenue { get; set; }
        public bool DifficultTrackAthletes { get; set; }
        public bool HardCommunicationWithAthletes { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }
}
