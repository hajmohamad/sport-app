using System.Threading.Tasks;
using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface ISmsService
{
    public Task<string> SendCode(string PhoneNumber);
    public Task<string> SendErrorSms(string message);
}
