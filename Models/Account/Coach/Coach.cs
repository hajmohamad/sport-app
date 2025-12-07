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
    [StringLength(11)]
    public required string PhoneNumber { get; set; }
    public List<CoachService> CoachingServices { get; set; } = [];
    public List<Payment>? Payments { get; set; } = [];
    public List<WorkoutProgram> WorkoutPrograms { get; set; } = [];
    public CoachQuestion? CoachQuestion { get; set; }   
    [StringLength(51)]
    public string InstagramLink { get; set; } = "";
    [StringLength(51)]
    public string TelegramLink { get; set; } = "";
    [StringLength(51)]
    public string WhatsApp { get; set; } = "";
    public bool Verified { get; set; } = false;
    public double Amount { get; set; } = 0;
    public double ServiceFee { get; set; } = 0.1;
    [StringLength(51)]
    public string? WebSiteUrl { get; set; } = "";

}
