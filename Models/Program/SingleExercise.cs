using sport_app_backend.Models.Actions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Program;
public class SingleExercise
{ [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int ProgramInDayId { get; set; }
    public int Set {get; set;}
    public int Rep { get; set; }
    public ProgramInDay? ProgramInDay { get; set; }


    [MaxLength(100)]
    public string? RepsJson { get; set; }

    [NotMapped]
    public required List<int> Reps
    {
        get => string.IsNullOrEmpty(RepsJson)
            ? new List<int>()
            : System.Text.Json.JsonSerializer.Deserialize<List<int>>(RepsJson)!;

        set => RepsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
    

    public Exercise? Exercise { get; set; }
    public int ExerciseId { get; set; }

    public required RepType? RepType { get; set; }
    [MaxLength(500)]

    public required string Description { get; set; } = "";
}


public enum RepType
{
    Count,
    Time,
    
}
