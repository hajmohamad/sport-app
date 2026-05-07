using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Dtos;

public class AddCardNumberDto
{
    [StringLength(20)]
    public required string CardName { get; set; }
    [StringLength(24)]
    public required string ShebaNumber { get; set; } 
}