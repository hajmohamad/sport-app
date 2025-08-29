using System.Threading.Tasks;
using sport_app_backend.Models;
using sport_app_backend.Services;

namespace sport_app_backend.Interface;

public interface ISmsService
{
    public Task<string> SendCode(string PhoneNumber);
    public Task<string> SendErrorSms(string message);
    public Task<string> CoachServiceBuySmsNotification(string phoneNumber, string name, string nameService,
        string price);
    public Task<SmsResponse> AthleteSuccessfullySmsNotification(string mobileNumber, string athleteName, string serviceName);
    public Task<SmsResponse> WorkoutReadySms(string mobileNumber, string athleteName, string serviceName);

    public Task<SmsResponse> AthleteSuccessfullySmsNotificationFromBuyFromSite(string mobileNumber, string wpKey,
        string serviceName);



}
