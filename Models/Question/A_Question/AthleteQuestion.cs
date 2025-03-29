using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Question.A_Question
{
    public class AthleteQuestion
    {
        [Key]
        public int Id { get; set; }
        public int AthleteId { get; set; }
        public Athlete? Athlete { get; set; }
        public InjuryArea? InjuryArea { get; set; }
        public FitnessLevel? FitnessLevel { get; set; }
        public int CurrentBodyForm { get; set; }
        public int TargetBodyForm { get; set; }
        public int DaysPerWeekToExercise { get; set; }
        public ExerciseGoal? ExerciseGoal { get; set; }
        public List<ExerciseMotivation>? ExerciseMotivation { get; set; }
        public List<CommonIssues>? CommonIssues { get; set; }
        
    }
}