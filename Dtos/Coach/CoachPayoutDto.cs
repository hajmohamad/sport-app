namespace sport_app_backend.Dtos;

public class CoachPayoutDto
{

    public double Amount { get; set; }
    public string RequestDate { get; set; }
    public string Status { get; set; }
    public string? PaidDate { get; set; }
    public string? TransactionReference { get; set; }

}