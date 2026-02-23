using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models;

public class InAppMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Title { get; set; }
    [MaxLength(300)]
    public required string Message { get; set; } 
    [MaxLength(200)]
    public string? ActionLink { get; set; } 

    [MaxLength(50)]
    public string? ActionText { get; set; } 
    public TypeOfUser TargetRole { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}