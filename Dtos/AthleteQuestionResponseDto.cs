using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Dtos
{
    public class AthleteQuestionResponseDto
    {
        public int DaysPerWeekToExercise { get; set; }
        public int CurrentBodyForm { get; set; }
        public double CurrentWeight { get; set; }
        public string? ExerciseGoal { get; set; }
        public InjuryAreaDto? InjuryArea { get; set; }
        public string? FitnessLevel { get; set; }
        public string? BirthDay { get; set; }
        public int AthleteBodyImageId { get; set; }
        public AthleteBodyImageDto? AthleteBodyImage { get; set; }
        public string ExerciseLocation { get; set; }
        public string ComingCompetition  { get; set; }= "";
        public string CompetitionHistory { get; set; }= "";
        public string CurrentMedications {get; set;}= "";
        public string SittingHour {get; set;}= "";
        public string YourJob {get; set;}= "";
        public string YourCity {get; set;}= "";
    }
}