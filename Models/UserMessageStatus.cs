using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Models;

public class UserMessageStatus
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public required int UserId { get; set; }
    public User User { get; set; } = null!;

    public required int InAppMessageId { get; set; }
    public InAppMessage InAppMessage { get; set; } = null!;

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}