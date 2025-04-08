using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Actions;

public class Exercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public required string EnglishName { get; set; }
    public required string PersianName { get; set; }

    public string SourceLink { get; set; } = string.Empty;
    public string ForeignAppLink { get; set; } = string.Empty;

    public string ImageLink { get; set; } = string.Empty;
    public string AnatomyImage { get; set; } = string.Empty;
    public List<string> AllImages { get; set; } = new();

    public string VideoLink { get; set; } = string.Empty;

    [MaxLength(1500)]
    public required string Description { get; set; }

    public ExerciseLevel? ExerciseLevel { get; set; }
    public List<MuscleGroup> TargetMuscles { get; set; } = new();
    public MuscleGroup BaseCategory { get; set; }

    public List<ExerciseCategory> ExerciseCategories { get; set; } = new();
    public List<EquipmentType> Equipment { get; set; } = new();

    public bool InGym { get; set; }
    public bool InHome { get; set; }

    public int Calories { get; set; } = 0;

    public string Location { get; set; } = string.Empty;
}
