namespace sport_app_backend.Infrastructure.Cache;

public static class CacheKeys
{
    public const string AllExercises = "exercises:all";

    public static string CoachPins(int coachId)
        => $"coach:{coachId}:pins";

    public static string CoachLastWorkout(int coachId, int athleteId)
        => $"coach:{coachId}:athlete:{athleteId}:last-workouts";

    public static string Athlete(int athleteId)
        => $"athlete:{athleteId}";

    public static string AthletePhone(string phone)
        => $"athlete-phone:{phone}";

    public static string ActiveWorkoutProgram(int athleteId)
        => $"active-workout-program:{athleteId}";

    public static string TrainingSession(int trainingSessionId)
        => $"training-session:{trainingSessionId}";

    public static string CoachChatListByUserId(int coachUserId)
        => $"chat:coach:list:user:{coachUserId}";

    public static string AthleteChatListByUserId(int athleteUserId)
        => $"chat:athlete:list:user:{athleteUserId}";

    public static string UserChatListVersion(int userId)
        => $"chat:list:version:user:{userId}";

    public static string GetCoachWorkoutProgram(int coachUserId)
        => $"coach-programs-:{coachUserId}";
}
