using sport_app_backend.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Program
{
    public class ExerciseChangeRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SingleExerciseId { get; set; }
        public SingleExercise? SingleExercise { get; set; }
        public ExerciseChangeReason Reason { get; set; }
        public int AthleteId { get; set; }
        public Athlete? Athlete { get; set; }
        public int CoachId { get; set; }
        public Coach? Coach { get; set; }
        public int TrainingSessionId { get; set; }
        public TrainingSession? TrainingSession { get; set; }
    }
}
