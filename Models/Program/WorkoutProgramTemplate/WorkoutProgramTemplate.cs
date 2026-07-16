using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account.Coach;

namespace sport_app_backend.Models.Program.WorkoutProgramTemplate;
public class WorkoutProgramTemplate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;

    [MaxLength(100)]
    public string Title { get; set; } = "";

    [MaxLength(250)]
    public string Description { get; set; } = "";

    public int ProgramDuration { get; set; }
    public ProgramLevel ProgramLevel { get; set; }
    public ProgramPriority ProgramPriorities { get; set; }

    public bool IsCompleted { get; set; } = false;

    public List<TemplateProgramInDay> ProgramInDays { get; set; } = [];
}