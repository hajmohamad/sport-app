using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Payments;

namespace sport_app_backend.Models.TrainingPlan;

public class CoachService
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int CoachId { get; set; }
    public Coach Coach {get; set;} 
    [MaxLength(50)]
    public required string Title { get; set; }
    [MaxLength(500)]
    public required string Description { get; set; }
    public required double Price { get; set; }
    public bool IsActive { get; set; }
    // public required CommunicateType CommunicateTypenicateType{get; set;}
    [DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime CreatedDate { get; set; }=DateTime.Now.Date;
    // public TypeOfCoachingServices TypeOfCoachingServices { get; set; }
    public bool IsDeleted { get; set; } = false;
    public  int NumberOfSell { get; set; } = 0;
    // public int? PublicDiscountPercent { get; set; }
    // public DateTime? PublicDiscountExpiresAt { get; set; }
    // public int? UsageLimit { get; set; }
    // public int? NumberOfSellWithDiscount { get; set; }
}
