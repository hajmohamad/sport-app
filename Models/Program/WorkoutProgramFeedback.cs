using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Program;

public class WorkoutProgramFeedback
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int CoachId { get; set; }
    public int AthleteId { get; set; }
    public int WorkoutProgramId { get; set; }
    public required WorkoutProgram WorkoutProgram { get; set; }
    public bool IsChosen { get; set; }
    [StringLength(200)] 
    public string WorkoutProgramName { get; set; } = "";
    [StringLength(500)] public string FeedBack { get; set; } = "";
    public int Score { get; set; }
    public DateTime Date { get; set; }= DateTime.Now.Date;
    [StringLength(30)]
    public string AthleteName { get; set; } = "";
    
}