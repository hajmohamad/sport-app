using System.Threading.Tasks;
using sport_app_backend.Services;

namespace sport_app_backend.Interface;

public interface ISmsService
{
    Task<string> SendCode(string phoneNumber);
    Task<string> SiteLogin(string phoneNumber);

    Task<SmsResponse> SupportTicketCreatedSms(string mobileNumber, string ticketTitle);
    Task<SmsResponse> SupportTicketAnsweredSms(string mobileNumber, string ticketTitle);

    Task<SmsResponse> CoachServiceBuySmsNotification(string phoneNumber, string coachName, string serviceName, string price);
    Task<SmsResponse> AthleteSuccessfullySmsNotification(string mobileNumber, string athleteName, string serviceName);
    Task<SmsResponse> NotifyAthleteOfProgramLinkSms(string mobileNumber, string athleteName, string link);

    Task<SmsResponse> AthleteSuccessfullySmsNotificationForBuyFromSite(string mobileNumber, string serviceName, string link);

    Task<SmsResponse> WorkoutReadySms(string mobileNumber, string athleteName, string serviceName, string link);

    Task<SmsResponse> SendPaymentAttemptSms(string phoneNumber, string serviceTitle, string coachName, string websiteUrl);
    Task<SmsResponse> SendProgramExpiredReminderSms(string phoneNumber, string athleteName, int daysSinceEnd, string websiteUrl);
    Task<SmsResponse> SendProgramSessionsReminderSms(string phoneNumber, string athleteName, int remainingSessions, string websiteUrl);
    Task<SmsResponse> SendQuestionReminderSms(string phoneNumber, string athleteName, string link);

    Task<string> SendErrorSms(string message);
}