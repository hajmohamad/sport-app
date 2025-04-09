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
        public string ImageLink { get; set; } = string.Empty;
        public string VideoLink { get; set; } = string.Empty;

        [MaxLength(1500)]
        public string Description { get; set; } = string.Empty;
        public string ExerciseLevel { get; set; } = string.Empty; // Beginner, Intermediate, Advanced
        public  List<string> TargetMuscles { get; set; } = new();
        public string BaseCategory { get; set; } = string.Empty;
        public  List<string> ExerciseCategories { get; set; } = [];
        public  List<string> Equipment { get; set; } = [];
        public List<string> Location { get; set; } = [];
    }
}