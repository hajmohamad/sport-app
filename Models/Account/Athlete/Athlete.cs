using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;


namespace sport_app_backend.Models.Account;

public class Athlete
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string PhoneNumber { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    [Range(1, 300)]
    public int Height { get; set; }
    [Range(1, 300)]
    public double CurrentWeight { get; set; }
    [Range(1, 300)]
    public double WeightGoal { get; set; }
    public List<WeightEntry> WeightEntries { get; set; } = [];
    public WaterInTake? WaterInTake { get; set; }
    public List<WaterInDay> WaterInDays { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<WorkoutProgram> WorkoutPrograms { get; set; } = [];
    public AthleteQuestion? AthleteQuestion { get; set; }
}
