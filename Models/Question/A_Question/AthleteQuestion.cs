using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Question.A_Question
{
    public class  AthleteQuestion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int AthleteId { get; set; }
        public Athlete? Athlete { get; set; }
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        public DateTime CreatedAt { get; set; }= DateTime.Now.Date;
        public InjuryArea? InjuryArea { get; set; }
        public FitnessLevel? FitnessLevel { get; set;}
        public int CurrentBodyForm { get; set; }
        public int DaysPerWeekToExercise { get; set; }
        public double Weight { get; set; }
        public ExerciseGoal? ExerciseGoal { get; set; }
        public List<ExerciseMotivation>? ExerciseMotivation { get; set; }
        public List<CommonIssues>? CommonIssues { get; set; }
        
    }
}