using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Challenge_Achievement
{
    public class Achievement
    {
        public int Id { get; set; }
        public int AthleteId { get; set; }
        public Athlete? Athlete { get; set; }
        public AchievementType AchievementType { get; set; }
        public DateTime UnlockedAt { get; set; } = DateTime.Now.Date;
    }
}
