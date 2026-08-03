using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sport_app_backend.Dtos.Eitaa;

public class EitaaCompleteLoginRequestDto
{
    [Required]
    public string LinkingToken { get; set; } = null!;

    [Required]
    public string Contact { get; set; } = null!;
}

