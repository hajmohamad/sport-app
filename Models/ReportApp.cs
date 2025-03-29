using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models;

public class ReportApp
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id {get; set;}
    public int UserId {get; set;}
    public User? User{get; set;}
    public string Category{get; set;}=string.Empty;
    public string Description{get; set;}=string.Empty;
}