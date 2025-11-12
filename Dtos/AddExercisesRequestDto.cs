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

        public int Id { get; set; }
        public required string EnglishName { get; set; }
        public required string PersianName { get; set; }
        public string ImageLink { get; set; } = string.Empty;
        public string VideoLink { get; set; } = string.Empty;
        public required string Description { get; set; }
        public double Met { get; set; }

        public string ExerciseLevel { get; set; } 
        public List<string> TargetMuscles { get; set; } = [];
        public string BaseMuscle { get; set; }
        public string BaseCategory { get; set; }
        public string Mechanics { get; set; }
        public ForceType ForceType { get; set; }
        public int Views { get; set; } = 0;
        public string ExerciseType { get; set; }
        public string Equipment { get; set; }
        public string Slug { get; set; } = string.Empty;
    }

    
}