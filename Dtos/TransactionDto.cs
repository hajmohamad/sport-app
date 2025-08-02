namespace sport_app_backend.Dtos;

public class TransactionDto
{
    public double Amount { get; set; }
    public string Type { get; set; } // "افزایش" or "کاهش"
    public string Date { get; set; }
    
    public string Description { get; set; }
    public double AppFee { get; set; }
    public string? BuyerName { get; set; }
    public string? ReferenceId { get; set; }
    public string? ProgramStatus { get; set; }
}