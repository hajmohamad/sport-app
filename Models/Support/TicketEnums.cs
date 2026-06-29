namespace sport_app_backend.Models.Support;

public enum TicketStatus
{
    Pending,   // در حال بررسی
    Answered,  // پاسخ داده شده
    Closed     // بسته شده
}

public enum TicketCategory
{
    TechnicalSupport,         // ارتباط با واحد فنی و راهنمای برنامه
    SuggestionsAndCriticisms,  // پیشنهادات و انتقادات
    Other                     // موارد دیگر
}