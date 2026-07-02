using System.Text;
using Newtonsoft.Json;
using sport_app_backend.Interface;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace sport_app_backend.Services;

public class SmsService(IConfiguration config) : ISmsService
{
    private readonly string _accessKey = config["SMS:accessKey"] ?? "deployMode";

    private const string LineNumber = "9981802897";
    private const string LikeToLikeUrl = "https://api.sms.ir/v1/send/likeToLike";
    private const string VerifyUrl = "https://api.sms.ir/v1/send/verify";

    private readonly HttpClient _httpClient = new();

    private async Task<SmsResponse> SendLikeToLikeSms(string phoneNumber, string message)
    {
        if (_accessKey == "deployMode")
        {
            return new SmsResponse { IsSuccess = true, Message = "deploy mode" };
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);

        var payload = new
        {
            LineNumber,
            MessageTexts = new[] { message },
            Mobiles = new[] { phoneNumber }
        };

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(LikeToLikeUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? new SmsResponse { IsSuccess = true, Message = result }
                : new SmsResponse { IsSuccess = false, Message = result };
        }
        catch (Exception ex)
        {
            return new SmsResponse
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task<string> SendCode(string phoneNumber)
    {
        if (_accessKey == "deployMode")
            return "12345";

        var random = new Random();
        var code = random.Next(10000, 100000).ToString();

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);

        var model = new VerifySendModel
        {
            Mobile = phoneNumber,
            TemplateId = 980201,
            Parameters =
            [
                new VerifySendParameterModel
                {
                    Name = "CODE",
                    Value = code
                }
            ]
        };

        var payload = JsonSerializer.Serialize(model);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        await _httpClient.PostAsync(VerifyUrl, content);

        return code;
    }

    public async Task<string> SiteLogin(string phoneNumber)
    {
        if (_accessKey == "deployMode")
            return "12345";

        var random = new Random();
        var code = random.Next(10000, 100000).ToString();

        var message =
            "کد ورود به سایت بدنسازی چارسِت\n" +
            $"Code:{code}\n" +
            "Chaarset.ir";

        await SendLikeToLikeSms(phoneNumber, message);

        return code;
    }

    public async Task<SmsResponse> SendSms(string phoneNumber, string message)
    {
        return await SendLikeToLikeSms(phoneNumber, message);
    }

    public async Task<SmsResponse> SupportTicketCreatedSms(string mobileNumber, string ticketTitle)
    {
        var message =
            $"کاربر گرامی، تیکت شما با موضوع «{ticketTitle}» در چارسِت ایجاد شد.";

        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> SupportTicketAnsweredSms(string mobileNumber, string ticketTitle)
    {
        var message =
            $"کاربر گرامی، تیکت شما با موضوع «{ticketTitle}» در چارسِت پاسخ داده شد.";

        return await SendLikeToLikeSms(mobileNumber, message);
    }


    public async Task<SmsResponse> CoachServiceBuySmsNotification(string phoneNumber, string name, string nameService, string price)
    {
        var message =
            $"{name} عزیز، یک نفر {nameService} رو ازت خریداری کرد.\n" +
            $"مبلغ {price} تومان به زودی دریافت می‌کنی.\n\n" +
            "chaarset.ir";

        return await SendLikeToLikeSms(phoneNumber, message);
    }

    public async Task<SmsResponse> AthleteSuccessfullySmsNotification(string mobileNumber, string athleteName, string serviceName)
    {
        var message =
            $"{athleteName} عزیز، درخواستت برای برنامه {serviceName} با موفقیت برای مربی ارسال شد.\n" +
            "برای مشاهده وضعیت برنامه وارد اپلیکیشن چارسِت شو.\n\n" +
            "chaarset.ir";

        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> NotifyAthleteOfProgramLinkSms(string mobileNumber, string athleteName, string wpkey)
    {
        var message =
            $"{athleteName} عزیز، درخواست شما برای مربی ارسال شد.\n" +
            $"chaarset.ir/program/{wpkey}";

        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> AthleteSuccessfullySmsNotificationForBuyFromSite(string mobileNumber, string wpKey, string serviceName)
    {
        var message =
            $"پرداخت شما برای سرویس «{serviceName}» با موفقیت انجام شد.\n\n" +
            $"chaarset.ir/program/{wpKey}/";

        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> WorkoutReadySms(string mobileNumber, string athleteName, string serviceName, string wpKey)
    {
        var message =
            $"{athleteName} عزیز، برنامه {serviceName} آماده شد.\n\n" +
            $"chaarset.ir/program/{wpKey}/";

        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<string> SendErrorSms(string message)
    {
        if (_accessKey == "deployMode")
            return "00000";

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);

        var model = new VerifySendModel
        {
            Mobile = "09395327229",
            TemplateId = 980201,
            Parameters =
            [
                new VerifySendParameterModel
                {
                    Name = "CODE",
                    Value = message
                }
            ]
        };

        var payload = JsonSerializer.Serialize(model);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        await _httpClient.PostAsync(VerifyUrl, content);

        return "00000";
    }
}

public class VerifySendParameterModel
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class VerifySendModel
{
    public string Mobile { get; set; }
    public int TemplateId { get; set; }
    public VerifySendParameterModel[] Parameters { get; set; }
}

public class SmsResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
}
