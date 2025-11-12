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
    //     public static Exercise ToExercises(this AddExercisesRequestDto addExercisesRequestDto)
    //     {
    //         Exercise exercise = new Exercise
    //         {
    //             EnglishName = addExercisesRequestDto.EnglishName,
    //             PersianName = addExercisesRequestDto.PersianName,
    //             ImageLink = addExercisesRequestDto.ImageLink ?? string.Empty,
    //             // AllImages = addExercisesRequestDto.AllImages ?? new List<string>(),
    //             VideoLink = addExercisesRequestDto.VideoLink ?? string.Empty,
    //             Description = addExercisesRequestDto.Description,
    //             ExerciseLevel = (ExerciseLevel)Enum.Parse(typeof(ExerciseLevel),
    //                 addExercisesRequestDto.ExerciseLevel.ToUpper()),
    //             TargetMuscles = addExercisesRequestDto.TargetMuscles
    //                 .Select(x => (MuscleGroup)Enum.Parse(typeof(MuscleGroup), x.ToUpper())).ToList(),
    //             BaseMuscle = (MuscleGroup)Enum.Parse(typeof(MuscleGroup), addExercisesRequestDto.BaseMuscle.ToUpper()),
    //             ExerciseType = (ExerciseType)Enum.Parse(typeof(ExerciseType), addExercisesRequestDto.ExerciseType.ToUpper()),
    //             Equipment = (EquipmentType)Enum.Parse(typeof(EquipmentType), addExercisesRequestDto.Equipment.ToUpper()),
    //             Mechanics = (MechanicType)Enum.Parse(typeof(MechanicType), addExercisesRequestDto.Mechanics),
    //             ForceType = addExercisesRequestDto.ForceType,
    //             Views = addExercisesRequestDto.Views
    //         };
    //         BaseCategory baseCategory;
    //         switch (exercise.BaseMuscle)
    //         {
    //             case MuscleGroup.ABS:
    //                 {
    //                     baseCategory = BaseCategory.ABSLOWERBACK;
    //                     break;
    //                 }
    //             case MuscleGroup.LOWER_BACK:
    //                 {
    //                     baseCategory = BaseCategory.ABSLOWERBACK;
    //                     break;
    //                 }
    //             case MuscleGroup.ADDUCTORS:
    //                 {
    //                     baseCategory = BaseCategory.ABDDUCTORS;
    //                     break;
    //                 }
    //             case MuscleGroup.ABDUCTORS:
    //                 {
    //                     baseCategory = BaseCategory.ABDDUCTORS;
    //                     break;
    //                 }
    //             case MuscleGroup.LATS:
    //                 {
    //                     baseCategory = BaseCategory.LATSUPPERBACK;
    //                     break;
    //                 }
    //             case MuscleGroup.UPPER_BACK:
    //                 {
    //                     baseCategory = BaseCategory.LATSUPPERBACK;
    //                     break;
    //                 }
    //             default:
    //                 {
    //                     baseCategory = (BaseCategory)Enum.Parse(typeof(BaseCategory), exercise.BaseMuscle.ToString());
    //                     break;
    //                 }
    //         }
    //         exercise.BaseCategory = baseCategory;
    //         return exercise;
    //     }
    //
       public static AllExerciseResponseDto ToAllExerciseResponseDto(this Exercise exercise)
        {
            return new Dtos.AllExerciseResponseDto
            {
                Id = exercise.Id,
                Name = exercise.PersianName,
                ImageLink = exercise.ImageLink,
                Mechanics = exercise.Mechanics.ToString(),
                BaseCategory = exercise.BaseCategory.ToString(),
                Equipment = exercise.Equipment.ToString(),
                ExerciseType = exercise.ExerciseType.ToString(),
                Level = exercise.ExerciseLevel.ToString(),
                View = exercise.Views,
                Met = exercise.Met ,
                BaseMuscle =  exercise.BaseMuscle.ToString(),
                
                
            };

        }

        public static ExerciseDto ToExerciseDto(this Exercise exercise)
        {
            return new ExerciseDto
            {
                Id = exercise.Id,
                Description = exercise.Description,
                ExerciseCategories = exercise.ExerciseType.ToString(),
                Equipment = exercise.Equipment.ToString(),
                Muscles = exercise.TargetMuscles.Select(x => x.ToString()).ToList(),
                Mechanics = exercise.Mechanics.ToString(),
                EnglishName = exercise.EnglishName,
                PersianName = exercise.PersianName,
                ImageLink = exercise.ImageLink,
                VideoLink = exercise.VideoLink,
                BaseCategory = exercise.BaseMuscle.ToString(),
                ExerciseLevel = exercise.ExerciseLevel.ToString(),
                Met = exercise.Met
            };
        }
        
        public static List<int> ToReps(this string? RepsJson)
        {
            return string.IsNullOrEmpty(RepsJson)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(RepsJson)!;
            
        }
        public static Exercise ToExercise(this AddExercisesRequestDto dto)
        {
            return new Exercise
            {
                Id = dto.Id,
                EnglishName = dto.EnglishName,
                PersianName = dto.PersianName,
                ImageLink = dto.ImageLink,
                VideoLink = dto.VideoLink,
                Description = dto.Description,
                Met = dto.Met,
                ExerciseLevel = ParseEnum<ExerciseLevel>(dto.ExerciseLevel, ExerciseLevel.BEGINNER),
                TargetMuscles = dto.TargetMuscles.Select(sl=>(MuscleGroup)Enum.Parse(typeof(MuscleGroup),sl)).ToList(),
                BaseMuscle = ParseEnum<MuscleGroup>(dto.BaseMuscle, MuscleGroup.BICEPS),
                BaseCategory = ParseEnum<BaseCategory>(dto.BaseCategory, BaseCategory.BICEPS),
                Mechanics = ParseEnum<MechanicType>(dto.Mechanics, MechanicType.COMPOUND),
                ForceType = dto.ForceType,
                Views = dto.Views,
                ExerciseType = ParseEnum<ExerciseType>(dto.ExerciseType, ExerciseType.CONDITIONING),
                Equipment = ParseEnum<EquipmentType>(dto.Equipment, EquipmentType.BANDS),
                Slug = dto.Slug
            };
        }
        private static T ParseEnum<T>(string value, T defaultValue) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return Enum.TryParse<T>(value, true, out var result) ? result : defaultValue;
        }

    }

}