using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.TrainingService;

namespace sport_app_backend.Models.Payments;

public class Payment
{   [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }  
    public required int AthleteId { get; set; }
    public Athlete Athlete { get; set; } = null!;
    public required int CoachId { get; set; }
    public Coach Coach { get; set; }  = null!;
    public double Amount {get; set;}
    public required int CoachServiceId { get; set; }
    public CoachService CoachService { get; set; }  = null!;
    [MaxLength(50)]
    public string Authority { get; set; }="";
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.INPROGRESS;
    public DateTime PaymentDate { get; set; }=DateTime.Now;
    public AthleteQuestion? AthleteQuestion { get; set; } 
    public  int? AthleteQuestionId { get; set; } 
    public  WorkoutProgram? WorkoutProgram { get; set; }
    public double AppFee { get; set; }
    public long RefId { get; set; }
}
