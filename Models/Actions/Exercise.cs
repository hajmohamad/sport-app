using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Actions;

public class Exercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(60)]
    public required string EnglishName { get; set; }
    [MaxLength(60)]
    public required string PersianName { get; set; }
    [MaxLength(170)]
    public string ImageLink { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string VideoLink { get; set; } = string.Empty;
    [MaxLength(3000)]
    public required string Description { get; set; }
    public double Met { get; set; }

    public ExerciseLevel ExerciseLevel { get; set; } = ExerciseLevel.BEGINNER;
    [MaxLength(100)]
    public List<MuscleGroup> TargetMuscles { get; set; } = [];
    public MuscleGroup BaseMuscle { get; set; }
    public BaseCategory BaseCategory { get; set; }
    public MechanicType Mechanics { get; set; }
    public ForceType ForceType { get; set; }
    public int Views { get; set; } = 0;
    public ExerciseType ExerciseType { get; set; }
    public EquipmentType Equipment { get; set; }
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;
}
