using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models;

public class WaterInDay
{   [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id {get; set;}
    public int AthleteId {get; set;}
    public Athlete? Athlete {get; set;}      
    public int NumberOfCupsDrinked {get; set;}
    [DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime Date {get; set;}
    
}
