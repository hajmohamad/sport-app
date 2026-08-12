using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models.Support;

public class TicketMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TicketId { get; set; }
    public SupportTicket Ticket { get; set; } = null!;

    [Required]
    public string MessageText { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsFromSupport { get; set; }

    public int? SenderId { get; set; } 
    [ForeignKey("SenderId")]
    public User? Sender { get; set; }
}