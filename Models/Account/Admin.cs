using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Models.Account;

public class Admin
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    public string FullName { get; set; } 
        
    public bool IsActive { get; set; } = true;
}