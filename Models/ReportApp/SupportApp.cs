using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models;

public class SupportApp
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id {get; set;}
    public int UserId {get; set;}
    public required User User{get; set;}
    public required List<SupportAppCategory> Category{get; set;}
    [MaxLength(300)]
    public required string Description{get; set;}=string.Empty;
}