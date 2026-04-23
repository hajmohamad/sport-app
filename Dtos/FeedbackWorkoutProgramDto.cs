using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Dtos;

public class FeedbackWorkoutProgramDto
{
    public int PaymentId { get; set; }
    [StringLength(500)] 
    public string FeedBack { get; set; } = "";
    public int Score { get; set; }
}