using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Question.A_Question;

public class AthleteBodyImage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int AthleteId  { get; set; }
    
    public int? AthleteQuestionId { get; set; }
    public AthleteQuestion? AthleteQuestion { get; set; }

    public  string? FrontLink { get; set; }
    public  string? BackLink { get; set; }
    public  string? SideLink { get; set; }


    
    
    
    
    
}