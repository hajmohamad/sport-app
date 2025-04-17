namespace sport_app_backend.Dtos;

public class AllPaymentResponseDto
{
    public required string PaymentStatus { get; set; }
    public required string Name { get; set; }
    public required string Amount { get; set; }
    public required string DateTime { get; set; }
    public string ImageProfile { get; set; }="";
    public int PaymentId { get; set; }
    public required string CoachServiceTitle { get; set; }
    public required string WorkoutProgramStatus { get; set; }
}