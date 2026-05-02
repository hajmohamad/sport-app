using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Models.Payments;

public class PaymentAttempt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }  
    public int CoachId { get; set; }
    public Coach Coach { get; set; } 
    public int CoachServiceId { get; set; }
    public CoachService CoachService { get; set; } 
    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; }
    public DateTime DateTime { get; set; }
    public bool SmsIsSend { get; set; }
    
    


}