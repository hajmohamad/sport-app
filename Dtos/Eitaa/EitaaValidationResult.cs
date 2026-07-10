using Microsoft.Extensions.Primitives;

namespace sport_app_backend.Dtos.Eitaa;

public class EitaaValidationResult
{
    public bool IsValid { get; set; }

    public string? Error { get; set; }

    public Dictionary<string, StringValues> Data { get; set; } = new();
}