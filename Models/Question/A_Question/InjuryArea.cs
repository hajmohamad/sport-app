using sport_app_backend.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Question.A_Question
{
    public class InjuryArea
    {
        [Key]
        public int Id { get; set; }
        public AthleteQuestion?  AthleteQuestion { get; set; }
        public int AthleteQuestionId { get; set; }
        public bool None { get; set; }  // آسیبی ندیده‌ام
        public List<SkeletalDiseases>? Skeletal { get; set; } //اسکلتی
        public List<SoftTissueAndLigamentInjuries>? SoftTissueAndLigament { get; set; }  // بافت نرم و رباطی
        public List<InternalAndDigestiveDiseases>? InternalAndDigestive { get; set; } //داخلی و گوارشی
        public List<HormonalAndGlandularDiseases>? HormonalAndGlandular { get; set; }   // هورمونی و غده‌ای
        public List<SpecificDiseases>? Specific { get; set; } // خاص
        [MaxLength(500)]
        public string? Others { get; set; }
    }
}