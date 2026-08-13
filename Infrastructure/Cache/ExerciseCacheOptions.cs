namespace sport_app_backend.Infrastructure.Cache;

public class ExerciseCacheOptions
{
    public const string SectionName = "ExerciseCache";

    public int AllExercisesTtlMinutes { get; set; } = 720;

    public int CoachPinsTtlMinutes { get; set; } = 60;

    public int LastWorkoutsTtlMinutes { get; set; } = 10;
}
