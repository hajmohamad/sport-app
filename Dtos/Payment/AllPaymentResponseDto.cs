namespace sport_app_backend.Dtos.Payment;

public class AllPaymentResponseDto
{
    public required string PaymentStatus { get; set; }
    public required string Name { get; set; }
    public required string Amount { get; set; }
    public required string DateTime { get; set; }
    public required string ImageProfile { get; set; }="";
    public int PaymentId { get; set; }
    public required string CoachServiceTitle { get; set; }
    public required string WorkoutProgramStatus { get; set; }
    public string WpKey { get; set; }
    public bool ShouldGetFeedback{ get; set; }
    public string CouchUrlSite { get; set; }
    public string? PaymentType { get; set; }
}