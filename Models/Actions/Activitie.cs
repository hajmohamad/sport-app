using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;


namespace sport_app_backend.Models.Actions;

public class Activitie
{   [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public ActivitiesEnum ActivityEnum;
    public double CaloriesLost { get; set; }
    public double Duration { get; set; }
    public double Distance { get; set; }
    public DateTime DateTime{ get; set; }
    public int AthleteId { get; set; }
    public required Athlete Athlete { get; set; }

   
}
