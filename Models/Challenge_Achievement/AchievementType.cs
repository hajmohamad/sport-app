namespace sport_app_backend.Models.Challenge_Achievement
{
    public enum AchievementType
    {
        FirstWorkout,         // اولین تمرین
        ConsistentAthlete,    // ورزشکار منظم (7 روز متوالی)
        FirstProgramDone,     // برنامه اول تمام شد
        OneMonthComplete,     // ماه اول تمام شد (30 روز)
        ThreeMonthsGolden,    // سه ماهه طلایی (90 روز)
        MasterAthlete,        // استادکار (365 روز)


        FirstChallenge,       // اولین چالش
        ChallengeSeeker,      // چالش پذیر (5 چالش مختلف)
        ChallengeMaster       // استاد چالش‌ها (12 چالش مختلف)
    }
}
