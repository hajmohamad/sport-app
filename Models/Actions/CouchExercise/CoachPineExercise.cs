using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Actions.CouchExercise;

public class CoachPineExercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int CoachId { get; set; }
    public Coach Coach { get; set; }
    public BaseCategory  BaseCategory { get; set; }
    
    private List<int> _exerciseIds = new();

    public List<int> ExerciseIds
    {
        get => _exerciseIds;
        set => _exerciseIds = value.Distinct().ToList();
    }






}