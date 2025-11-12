using System.ComponentModel.DataAnnotations;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Payments;

public class CoachPayout
{
    [Key]
    public int Id { get; set; }

    public int CoachId { get; set; }
    public required Coach Coach { get; set; }
    public double Amount { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.Now;
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public DateTime? PaidDate { get; set; }
    [MaxLength(100)] 
    public string? Imagelink { get; set; } = "";
    
    [MaxLength(100)]
    public string? TransactionReference { get; set; }
}
public enum PayoutStatus
{
    
    Pending,  
    Approved,  
    Paid,   
    Rejected 
}