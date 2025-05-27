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

        [MaxLength(3000)]
        public string Description { get; set; } = string.Empty;
        public string ExerciseLevel { get; set; } = string.Empty; // Beginner, Intermediate, Advanced
        public  List<string> TargetMuscles { get; set; } = new();
        public string BaseMuscle { get; set; } = string.Empty;
        public  string ExerciseType { get; set; } = string.Empty;
        public  string Equipment { get; set; } = string.Empty;
        public string Mechanics { get; set; } = string.Empty;
        public string ForceType { get; set; } = string.Empty;
        public int Views { get; set; }
    }
}