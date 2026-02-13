using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Account.Athlete;

public class WeightEntry
{
    [Key]
    public int Id { get; set; }
    public required int AthleteId { get; set; }
    public required Account.Athlete.Athlete Athlete {get; set;}
    [Required]
    [Range(1, 300)]
    public double Weight { get; set; }
    [Required]
    [DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime CurrentDate { get; set; }

}
