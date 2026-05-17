using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sport_app_backend.Models.Account.Coach;

public class CoachCardNumber
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CoachId { get; set; }

    public Coach Coach { get; set; } = null!;

    [StringLength(20)]
    public required string CardName { get; set; }

    [StringLength(24)]
    public required string ShebaNumber { get; set; }
}