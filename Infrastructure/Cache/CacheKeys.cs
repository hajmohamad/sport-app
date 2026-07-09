namespace sport_app_backend.Infrastructure.Cache;

public static class CacheKeys
{
    public const string AllExercises = "all_exercises";

    public static string CoachPins(int coachId)
        => $"coach_pins_{coachId}";

    public static string CoachLastWorkout(int coachId)
        => $"coach_lastWorkout_{coachId}";
    public static string Athlete(int athleteId)
        => $"athlete:{athleteId}";

    public static string AthletePhone(string phone)
        => $"athlete-phone:{phone}";

    public static string ActiveWorkoutProgram(int athleteId)
        => $"active-workout-program:{athleteId}";

    public static string TrainingSession(int trainingSessionId)
        => $"training-session:{trainingSessionId}";
    
}