using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Models.Account;

public class WeightEntry
{
    [Key]
    public int Id { get; set; }
    public required int AthleteId { get; set; }
    public required Athlete Athlete {get; set;}
    [Required]
    [Range(1, 300)]
    public double Weight { get; set; }
    [Required]
    [DataType(DataType.Date)]
    public DateTime CurrentDate { get; set; }

}
