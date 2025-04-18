using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.TrainingService;

namespace sport_app_backend.Models.Account;

public class CoachService
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int CoachId { get; set; }
    public required Coach Coach {get; set;} 
    [MaxLength(50)]
    public required string Title { get; set; }
    [MaxLength(500)]
    public required string Description { get; set; }
    public required double Price { get; set; }
    public bool IsActive { get; set; }
    public bool HaveSupport{get; set;}
    [MaxLength(100)]
    public required string CommunicateType{get; set;}
    [DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime CreatedDate { get; set; }=DateTime.Now.Date;
    // public TypeOfCoachingServices TypeOfCoachingServices { get; set; }
    public bool IsDeleted { get; set; } = false;
    public  int NumberOfSell { get; set; } = 0;
}
