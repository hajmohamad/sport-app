using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Dtos
{
    public class AthleteQuestionDto
    {
        public int DaysPerWeekToExercise { get; set; }
        public int CurrentBodyForm { get; set; }
        public int TargetBodyForm { get; set; }
        public string? ExerciseGoal { get; set; }
        public InjuryAreaDto? InjuryArea { get; set; }
        public string? FitnessLevel { get; set; }

    }
}