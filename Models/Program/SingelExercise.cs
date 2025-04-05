using sport_app_backend.Models.Actions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Program;

public class SingelExercise
{   [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int ProgramInDayId { get; set; }
    public ProgramInDay? ProgramInDay { get; set; }
    public int Set { get; set; }
    public int Rep { get; set; }
    public Exercise? Exercise { get; set; }
    public int ExerciseId { get; set; }

}
