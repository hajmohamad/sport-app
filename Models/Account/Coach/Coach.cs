using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Models.Account;

public class Coach
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int UserId { get; set; }
    public required User User { get; set; }
    public required string PhoneNumber { get; set; }
    [EmailAddress]
    [StringLength(50)]
    public string Email { get; set; } = "";
    public List<CoachingDomain>? Domain { get; set; }
    public int StartCoachingYear { get; set; }
    public List<CoachService> CoachingServices { get; set; } = [];
    public List<Payment>? Payments { get; set; } = [];
    public List<WorkoutProgram> WorkoutPrograms { get; set; } = [];
   
    public CoachQuestion? CoachQuestion { get; set; }
    [MaxLength(124)]
    public string HeadLine { get; set; } = "";

}
