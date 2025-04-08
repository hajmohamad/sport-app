namespace sport_app_backend.Dtos;

public class AllExerciseResponseDto
{
    public required string Name { get; set; }
    public required string ImageLink { get; set; }
    public required List<string > Locations { get; set; }
    public required List<string > Muscles { get; set; }
    public required List<string > Equipment { get; set; }
}