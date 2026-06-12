using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.SupportApp;

public class SupportApp
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id {get; set;}
    public int UserId {get; set;}
    public required User User{get; set;}
    public required SupportAppCategory Category{get; set;}
   // public DateTime CreateDate { get; set; }=DateTime.Now;
    [MaxLength(300)]
    public required string Description{get; set;}=string.Empty;

    public bool IsActive { get; set; } = true;
}