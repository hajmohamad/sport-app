using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Payments;

namespace sport_app_backend.Models.Program;

public class WorkoutProgram
{   [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int CoachId { get; set; }
    public Coach? Coach {get; set;}
    public int AthleteId { get; set; }
    public Athlete? Athlete {get; set;}
    public string Title { get; set; }="";
    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public WorkoutProgramStatus Status { get; set; }
    public int Duration { get; set; }
    public string Description { get; set; } = "";
    public List<WorkoutProgramInDay>? WorkoutProgramInDays { get; set; }    
}
