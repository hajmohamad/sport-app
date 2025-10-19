using System.Threading.Tasks;
using sport_app_backend.Models;
using sport_app_backend.Services;

namespace sport_app_backend.Interface;

public interface ISmsService
{
    public Task<string> SendCode(string PhoneNumber);
    public Task<string> SendErrorSms(string message);
    public Task<SmsResponse> CoachServiceBuySmsNotification(string phoneNumber, string name, string nameService,
        string price);
    public Task<SmsResponse> AthleteSuccessfullySmsNotification(string mobileNumber, string athleteName, string serviceName);
    public Task<SmsResponse> WorkoutReadySms(string mobileNumber, string athleteName, string serviceName,string wpKey);

    public Task<SmsResponse> AthleteSuccessfullySmsNotificationForBuyFromSite(string mobileNumber, string wpKey,
        string serviceName);

    public Task<SmsResponse> NotifyAthleteOfProgramLinkSms(string mobileNumber, string athleteName, string programLink);

    public Task<SmsResponse> SendSms(string phoneNumber, string message);

}
