using sport_app_backend.Models.Actions;

namespace sport_app_backend.Dtos;

public class AllExerciseResponseDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageLink { get; set; }
    public required string BaseCategory { get; set; }
    public required string Equipment { get; set; }
    public required string ExerciseType { get; set; }
    public required string Level { get; set; }
    public required string Mechanics { get; set; }
    public required int View {get; set; }
    public required double Met { get; set; }
    public string BaseMuscle { get; set; }
}