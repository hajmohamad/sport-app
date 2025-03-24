using sport_app_backend.Interface;

namespace sport_app_backend.Services;

public class SendVerifyCodeService : ISendVerifyCodeService
{
   public async Task<string> SendCode(string PhoneNumber)
    {
        return "12345";
    }

}
