using System.ComponentModel.DataAnnotations;
using sport_app_backend.Models.Account.Coach;

namespace sport_app_backend.Models.Payments;

public class WalletTransaction
{
    public int Id { get; set; }
    public int CoachId { get; set; }
    public Coach Coach { get; set; }
    public double Amount { get; set; }
    public WalletTransactionStatus TransactionStatus { get; set; } = WalletTransactionStatus.INPROGRESS;
    [MaxLength(50)]
    public string Authority { get; set; }="";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ReferenceId { get; set; } //
}
public enum WalletTransactionStatus
{
    SUCCESS,
    FAILED,
    INPROGRESS,
}
