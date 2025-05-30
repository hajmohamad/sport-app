namespace sport_app_backend.Models.Actions
{
    public enum ForceType
    {
        PULL_BILATERAL,      // کشیدن (دوطرفه)
        PULL_UNILATERAL,     // کشیدن (یک‌طرفه)
        PUSH_BILATERAL,      // فشار دادن (دوطرفه)
        PUSH_UNILATERAL,     // فشار دادن (یک‌طرفه)
        HINGE_BILATERAL,     // خم شدن (دوطرفه)
        HINGE_UNILATERAL,    // خم شدن (یک‌طرفه)
        ISOMETRIC,           // ایزومتریک (ثابت بدون حرکت)
        DYNAMIC_STRETCHING,  // کشش پویا
        STATIC,              // کشش ایستا
        PUSH,                // فشار دادن
        PULL,                 // کشیدن
        NONE
    }
}
