using System.ComponentModel.DataAnnotations;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.C_Question;

namespace sport_app_backend.Models;

public class CoachQuestion
{   [Key]
    public int Id{get; set;}
    public int UserId{get; set;}
    public User? User{get; set;}
    public List<CoachDispline> Disciplines {get; set;}=[];
    public List<CoachingMotivation> Motivations {get; set;}=[];
    public bool WorkOnlineWithAthletes {get; set;}
    public List<PresentPracticeProgram> PresentsPracticeProgram {get; set;}=[];
    public TrackAthlete? TrackAthlete {get; set;} // trackAthletes
    public bool? ManagingRevenue {get; set;} // interval managingRevenue: Boolean,
    //q7
    public bool DifficultTrackAthletes {get; set;}
    //q8
    public bool HardCommunicationWithAthletes {get; set;}
}
