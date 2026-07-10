using System.ComponentModel.DataAnnotations;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.UserExternalAccount;

public class UserExternalAccount
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    [MaxLength(50)]
    public Provider Provider { get; set; } 

    [MaxLength(100)]
    public string ProviderUserId { get; set; } = null!;

    [MaxLength(100)]
    public string? ProviderUsername { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}

public enum Provider
{
    Eita
}