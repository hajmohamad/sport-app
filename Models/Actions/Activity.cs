using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;


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
    public DateTime Date { get; set; }=DateTime.Now;
    public required int AthleteId { get; set; }
    public Athlete Athlete { get; set; }

   
}
