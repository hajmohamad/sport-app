using sport_app_backend.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Challenge_Achievement
{
    public class Challenge
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int AthleteId { get; set; }
        public Athlete? Athlete { get; set; }
        public ChallengeType ChallengeType { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now.Date;
    }
}
