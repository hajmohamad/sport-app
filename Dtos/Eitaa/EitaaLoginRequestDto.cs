using System.ComponentModel.DataAnnotations;

namespace sport_app_backend.Dtos.Eitaa;

public class EitaaLoginRequestDto
{
    [Required]
    public string InitData { get; set; } = null!;
}