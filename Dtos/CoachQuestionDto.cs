using sport_app_backend.Models.Account;
using sport_app_backend.Models.C_Question;

namespace sport_app_backend.Dtos
{
    public class CoachQuestionDto
    {
        public Gender Gender { get; set; }
        public List<CoachDispline> Disciplines { get; set; } = [];
        public List<CoachingMotivation> Motivations { get; set; } = [];
        public bool WorkOnlineWithAthletes { get; set; }
        public List<PresentPracticeProgram> PresentsPracticeProgram { get; set; } = [];
        public TrackAthlete TrackAthlete { get; set; }
        public bool ManagingRevenue { get; set; }
        public bool DifficultTrackAthletes { get; set; }
        public bool HardCommunicationWithAthletes { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }
}
