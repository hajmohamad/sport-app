using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Models.Payments;

public class DiscountCode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;
    [MaxLength(32)]
    public required string Code { get; set; }

    public int DiscountPercent { get; set; }

    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DiscountCodeStatus Status { get; set; } = DiscountCodeStatus.ACTIVE;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
    public List<Payment> Payments { get; set; } = [];
    public List<int> CoachServicesId { get; set; } = [];
}
