using System.Text.Json.Serialization;

namespace sport_app_backend.Dtos.Eitaa;

public class EitaaUserDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; set; }

    [JsonPropertyName("allows_write_to_pm")]
    public bool? AllowsWriteToPm { get; set; }
}