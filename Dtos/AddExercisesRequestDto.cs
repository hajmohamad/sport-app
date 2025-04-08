using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Dtos
{
    public class AddExercisesRequestDto
    {
        public string EnglishName { get; set; } = string.Empty;
        public string PersianName { get; set; } = string.Empty;

        public string SourceLink { get; set; } = string.Empty;
        public string ForeignAppLink { get; set; } = string.Empty;

        public string ImageLink { get; set; } = string.Empty;
        public string AnatomyImage { get; set; } = string.Empty;

        public required List<string> AllImages { get; set; } = new();

        public string VideoLink { get; set; } = string.Empty;

        [MaxLength(1500)]
        public string Description { get; set; } = string.Empty;

        public string ExerciseLevel { get; set; } = string.Empty; // Beginner, Intermediate, Advanced
        public required List<string> TargetMuscles { get; set; } = new();
        public string BaseCategory { get; set; } = string.Empty;

        public required List<string> ExerciseCategories { get; set; } = new();
        public required List<string> Equipment { get; set; } = new();

        public bool InGym { get; set; }
        public bool InHome { get; set; }

        public int Calories { get; set; } = 0;

        public string Location { get; set; } = string.Empty;
    }
}