using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Models.Account;

public class CoachPlan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int CoachId { get; set; }
    public Coach? Coach {get; set;} 
    [MaxLength(50)]
    public required string Title { get; set; }
    [MaxLength(500)]
    public required string Description { get; set; }
    public required double Price { get; set; }
    public bool IsActive { get; set; }
    public bool HaveSupport{get; set;}
    public required string CommunicateType{get; set;}
    public DateTime CreatedDate { get; set; }=DateTime.Now;
    public TypeOfCoachingPlan TypeOfCoachingPlan { get; set; }
    public bool IsDeleted { get; set; } = false;
}
