using sport_app_backend.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Question.A_Question
{
    public class InjuryArea
    {
        [Key]
        public int Id { get; set; }
        public int AthleteId { get; set; }
        public Athlete? Athlete { get; set; }
        public bool none { get; set; }  // آسیبی ندیده‌ام
        public List<SkeletalDiseases>? skeletal { get; set; } //اسکلتی
        public List<SoftTissueAndLigamentInjuries>? softTissueAndLigament { get; set; }  // بافت نرم و رباطی
        public List<InternalAndDigestiveDiseases>? internalAndDigestive { get; set; } //داخلی و گوارشی
        public List<HormonalAndGlandularDiseases>? hormonalAndGlandular { get; set; }   // هورمونی و غده‌ای
        public List<SpecificDiseases>? specific { get; set; } // خاص
        public string? others { get; set; }
    }
}