using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Mappers
{
    public static class ExercisesMappers
    {
        public static Exercise ToExercises(this AddExercisesRequestDto addExercisesRequestDto)
        {
            return new Exercise
            {
                EnglishName = addExercisesRequestDto.EnglishName,
                PersianName = addExercisesRequestDto.PersianName,
                // SourceLink = addExercisesRequestDto.SourceLink ?? string.Empty,
                // ForeignAppLink = addExercisesRequestDto.ForeignAppLink ?? string.Empty,
                ImageLink = addExercisesRequestDto.ImageLink ?? string.Empty,
                // AnatomyImage = addExercisesRequestDto.AnatomyImage ?? string.Empty,
                // AllImages = addExercisesRequestDto.AllImages ?? new List<string>(),
                VideoLink = addExercisesRequestDto.VideoLink ?? string.Empty,
                Description = addExercisesRequestDto.Description,
                ExerciseLevel = (ExerciseLevel)Enum.Parse(typeof(ExerciseLevel),
                    addExercisesRequestDto.ExerciseLevel.ToUpper()),
                TargetMuscles = addExercisesRequestDto.TargetMuscles
                    .Select(x => (MuscleGroup)Enum.Parse(typeof(MuscleGroup), x.ToUpper())).ToList(),
                BaseCategory =
                    (MuscleGroup)Enum.Parse(typeof(MuscleGroup), addExercisesRequestDto.BaseCategory.ToUpper()),
                ExerciseCategories = addExercisesRequestDto.ExerciseCategories
                    .Select(x => (ExerciseCategory)Enum.Parse(typeof(ExerciseCategory), x.ToUpper())).ToList(),
                Equipment = addExercisesRequestDto.Equipment
                    .Select(x => (EquipmentType)Enum.Parse(typeof(EquipmentType), x.ToUpper())).ToList(),
                // InGym = addExercisesRequestDto.InGym,
                // InHome = addExercisesRequestDto.InHome,
                // Calories = addExercisesRequestDto.Calories,
                Locations = addExercisesRequestDto.Location
                    .Select(x => (Location)Enum.Parse(typeof(Location), x.ToUpper())).ToList()
            };
        }

        public static AllExerciseResponseDto ToAllExerciseResponseDto(this Exercise exercise)
        {
            return new AllExerciseResponseDto
            {
                Id = exercise.Id,
                Name = exercise.PersianName,
                ImageLink = exercise.ImageLink,
                Locations = exercise.Locations.Select(x => x.ToString()).ToList(),
                Muscles = exercise.TargetMuscles.Select(x => x.ToString()).ToList(),
                Equipment = exercise.Equipment.Select(x => x.ToString()).ToList(),
                ExerciseCategories = exercise.ExerciseCategories.Select(x => x.ToString()).ToList(),
                Level = exercise.ExerciseLevel.ToString()
            };

        }

        public static ExerciseDto ToExerciseDto(this Exercise exercise)
        {
            return new ExerciseDto
            {
                Id = exercise.Id,
                Description = exercise.Description,
                ExerciseCategories = exercise.ExerciseCategories.Select(x => x.ToString()).ToList(),
                Equipment = exercise.Equipment.Select(x => x.ToString()).ToList(),
                Locations = exercise.Locations.Select(x => x.ToString()).ToList(),
                Muscles = exercise.TargetMuscles.Select(x => x.ToString()).ToList(),
                EnglishName = exercise.EnglishName,
                PersianName = exercise.PersianName,
                ImageLink = exercise.ImageLink,
                VideoLink = exercise.VideoLink,
                BaseCategory = exercise.BaseCategory.ToString(),
                ExerciseLevel = exercise.ExerciseLevel.ToString()
            };
        }
    }

}