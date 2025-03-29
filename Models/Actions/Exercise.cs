using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Actions;

public class Exercise
{   [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string EnglishName { get; set; }
    public required string PersianName { get; set; }
    public string MainImage { get; set; } = string.Empty;
    public string AnatomyImage { get; set; } = string.Empty;

    [MaxLength(500)]
    public required string Description { get; set; }
    public int Calories { get; set; }
    // Intermediate ,Beginner ,Advanced ,Unknown
    public string? ExerciseLevel{get;set;}
    public required List<string> Image { get; set; }
    public MuscleGroup BaseCategory { get; set; }
    public required List<MuscleGroup> ActionsTage { get; set; }
   
}
