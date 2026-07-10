using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Models.UserExternalAccount;

public class EitaaLoginSession
{
    public long Id { get; set; }

    [MaxLength(128)]
    public string TokenHash { get; set; } = null!;

    [MaxLength(100)]
    public string EitaaUserId { get; set; } = null!;

    [MaxLength(100)]
    public string? EitaaUsername { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }
}