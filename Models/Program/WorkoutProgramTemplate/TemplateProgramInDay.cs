using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Program.WorkoutProgramTemplate;

public class TemplateProgramInDay
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int WorkoutProgramTemplateId { get; set; }
    public WorkoutProgramTemplate? WorkoutProgramTemplate { get; set; }

    public int ForWhichDay { get; set; }

    public List<TemplateSingleExercise> AllExerciseInDays { get; set; } = [];
}