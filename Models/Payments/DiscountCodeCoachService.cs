using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Models.Payments;

public class DiscountCodeCoachService
{
    public int DiscountCodeId { get; set; }
    public DiscountCode DiscountCode { get; set; } = null!;

    public int CoachServiceId { get; set; }
    public CoachService CoachService { get; set; } = null!;
}