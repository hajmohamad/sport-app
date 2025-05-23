using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Actions;

public class Exercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(30)]
    public required string EnglishName { get; set; }
    [MaxLength(30)]
    public required string PersianName { get; set; }
    [MaxLength(150)]
    public string ImageLink { get; set; } = string.Empty;
    [MaxLength(150)]
    public string VideoLink { get; set; } = string.Empty;
    [MaxLength(1500)]
    public required string Description { get; set; }

    public ExerciseLevel ExerciseLevel { get; set; } = ExerciseLevel.BEGINNER;
    [MaxLength(150)]
    public List<MuscleGroup> TargetMuscles { get; set; } = [];
    public MuscleGroup BaseCategory { get; set; }
    public MechanicType Mechanics { get; set; }
    public ForceType ForceType { get; set; }
    public int Views { get; set; } = 0;
    public List<ExerciseCategory> ExerciseCategories { get; set; } = [];
    public List<EquipmentType> Equipment { get; set; } = [];
    public List<Location> Locations { get; set; } = [];
}
