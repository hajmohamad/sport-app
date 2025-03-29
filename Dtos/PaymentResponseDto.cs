namespace sport_app_backend.Dtos;

public class PaymentResponseDto
{
    public required string transactionId { get; set; }
    public required string PaymentStatus { get; set; }
    public required string Name { get; set; }
    public required string Amount { get; set; }
    public required string DateTime { get; set; }
    public byte[] ImageProfile { get; set; }=[];
    
    
    
}