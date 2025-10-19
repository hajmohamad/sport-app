using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Dtos;

public class PaymentResponseDto
{   public required int PaymentId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public required string ImageProfile { get; set; }= string.Empty;
    public string Amount { get; set; }= string.Empty;
    public string DateTime { get; set; }= string.Empty;
    public string PaymentStatus { get; set; }= string.Empty;
    public AthleteQuestionResponseDto? AthleteQuestion { get; set; }
    public required string? Gender { get; set; }
    public int Height { get; set; }
    
    public WorkoutProgramResponseDto? WorkoutProgram { get; set; }
    public required string BirthDate { get; set; }= string.Empty;
    public required string PdfLink { get; set; }
    public string WpKey {get; set;}= string.Empty;
}