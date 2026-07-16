using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Actions;

namespace sport_app_backend.Models.Program.WorkoutProgramTemplate;

public class TemplateSingleExercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TemplateProgramInDayId { get; set; }
    public TemplateProgramInDay? TemplateProgramInDay { get; set; }

    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    public RepType RepType { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = "";

    public string? RepsJson { get; set; }

    [NotMapped]
    public List<int> Reps
    {
        get => string.IsNullOrWhiteSpace(RepsJson)
            ? []
            : System.Text.Json.JsonSerializer.Deserialize<List<int>>(RepsJson!) ?? [];
        set => RepsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}