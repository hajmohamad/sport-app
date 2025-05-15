using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;


namespace sport_app_backend.Models.Actions;

public class Activity
{   [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Name { get; set; } = "";
    public ActivityCategory ActivityCategory {get; set;}
    public double CaloriesLost { get; set; }
    public double Duration { get; set; }
    public double Distance { get; set; }
    [DataType(DataType.Date)]
    [Column(TypeName = "date")]
    public DateTime DateTime { get; set; }=DateTime.Now.Date;
    public int AthleteId { get; set; }
    public required Athlete Athlete { get; set; }

   
}
