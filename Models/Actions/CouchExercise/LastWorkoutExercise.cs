using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Models.Actions.CouchExercise;

public class LastWorkoutExercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CoachId { get; set; }
    public Coach Coach { get; set; }

    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; }  

    public int WorkoutProgramId { get; set; }
    public WorkoutProgram WorkoutProgram { get; set; }

    private List<int> _exerciseIds = new();

    public List<int> ExerciseIds
    {
        get => _exerciseIds;
        set => _exerciseIds = value.Distinct().ToList();
    }
}