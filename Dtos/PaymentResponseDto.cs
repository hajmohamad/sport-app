using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Dtos;

public class PaymentResponseDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Amount { get; set; }= string.Empty;
    public string DateTime { get; set; }= string.Empty;
    public string PaymentStatus { get; set; }= string.Empty;
    public AthleteQuestionDto? AthleteQuestion { get; set; }
    public int Height { get; set; }
    
    public WorkoutProgramResponseDto? WorkoutProgram { get; set; }
}