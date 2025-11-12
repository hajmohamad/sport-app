using System.Text;
using Newtonsoft.Json;
using sport_app_backend.Interface;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace sport_app_backend.Services;

public class SmsService(IConfiguration config) : ISmsService
{
    private readonly string _accessKey = config["SMS:accessKey"] ?? "deployMode";

    public async Task<string> SendCode(string phoneNumber)
    {
        if (_accessKey == "deployMode")
        {
            return "12345";
        }

        var httpClient = new HttpClient();
        var random = new Random();
        var randomNumber = random.Next(10000, 100000).ToString();
        httpClient.DefaultRequestHeaders.Add("x-api-key",
            _accessKey);

        var model = new VerifySendModel()
        {
            Mobile = phoneNumber,
            TemplateId = 980201,
            Parameters =
            [
                new VerifySendParameterModel
                {
                    Name = "CODE", Value = randomNumber
                }
            ]
        };

        var payload = JsonSerializer.Serialize(model);
        StringContent stringContent = new(payload, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://api.sms.ir/v1/send/verify", stringContent);
        return randomNumber;
    }

    public async Task<SmsResponse> CoachServiceBuySmsNotification(string phoneNumber, string name, string nameService,
        string price)
    {
        var message = $"{name} عزیز، یک نفر {nameService} رو ازت خریداری کرد.\n" +
                      $"میتونی همین الان در عرض چند دقیقه برنامه رو طراحی و مبلغ {price} تومان رو دریافت کنی.\n\n" +
                      "chaarset.ir";
        
        const string lineNumber = "9981802897"; 
        const string apiUrl = "https://api.sms.ir/v1/send/likeToLike"; 

        var payload = new
        {
            LineNumber = lineNumber,
            MessageTexts = new[] { message },
            Mobiles = new[] { phoneNumber }
        };
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(apiUrl, content);
            var resultString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new SmsResponse { IsSuccess = true, Message = "پیامک با موفقیت ارسال شد." };
            }
            else
            {
              
                return new SmsResponse { IsSuccess = false, Message = $"خطا در ارسال پیامک: {response.StatusCode} - {resultString}" };
            }
        }
        catch (Exception ex)
        {
            return new SmsResponse { IsSuccess = false, Message = $"خطای غیرمنتظره: {ex.Message}" };
        }
    }
    public async Task<SmsResponse> AthleteSuccessfullySmsNotification(string mobileNumber, string athleteName, string serviceName)
    {
        var message = $"{athleteName} عزیز، درخواستت برای برنامه {serviceName} با موفقیت برای مربی ارسال شد.\n" +
                      "برای مشاهده وضعیت برنامه‌ات می‌تونی به قسمت برنامه‌ها در حساب کاربریت در اپلیکیشن چارسِت سر بزنی.\n\n" +
                      "chaarset.ir";

        const string lineNumber = "9981802897";
        const string apiUrl = "https://api.sms.ir/v1/send/likeToLike";


        var payload = new SmsPayload
        {
            LineNumber = lineNumber,
            MessageTexts = [message],
            Mobiles = [mobileNumber]
        };
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);


        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(apiUrl, content);
            var resultString = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? new SmsResponse { IsSuccess = true, Message = resultString }
                : new SmsResponse { IsSuccess = false, Message = $"API Error: {response.StatusCode} - {resultString}" };
        }
        catch (Exception ex)
        {
            return new SmsResponse { IsSuccess = false, Message = $"An exception occurred: {ex.Message}" };
        }
    }
    public async Task<SmsResponse> NotifyAthleteOfProgramLinkSms(string mobileNumber, string athleteName, string wpkey)
    {
        var message = $"{athleteName} عزیز، درخواست شما برای مربی ارسال شد.\nلینک زیر مخصوص برنامه تمرینی شماست و همیشه از طریق همین لینک به آن دسترسی خواهید داشت:\nchaarset.ir/program/{wpkey}";

        const string lineNumber = "9981802897"; // Your SMS line number
        const string apiUrl = "https://api.sms.ir/v1/send/likeToLike"; // The API endpoint

        var payload = new
        {
            LineNumber = lineNumber,
            MessageTexts = new[] { message },
            Mobiles = new[] { mobileNumber }
        };

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(apiUrl, content);
            var resultString = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode ? new SmsResponse { IsSuccess = true, Message = "پیامک اطلاع‌رسانی لینک با موفقیت ارسال شد." } :
                new SmsResponse { IsSuccess = false, Message = $"خطا در ارسال پیامک: {response.StatusCode} - {resultString}" };
        }
        catch (Exception ex)
        {
            return new SmsResponse { IsSuccess = false, Message = $"خطای غیرمنتظره: {ex.Message}" };
        }
    }

    public async Task<SmsResponse> SendSms(string phoneNumber, string message)
    {
        const string lineNumber = "9981802897"; 
        const string apiUrl = "https://api.sms.ir/v1/send/likeToLike"; 

        var payload = new
        {
            LineNumber = lineNumber,
            MessageTexts = new[] { message },
            Mobiles = new[] { phoneNumber }
        };
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(apiUrl, content);
            var resultString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new SmsResponse { IsSuccess = true, Message = "پیامک با موفقیت ارسال شد." };
            }
            else
            {
              
                return new SmsResponse { IsSuccess = false, Message = $"خطا در ارسال پیامک: {response.StatusCode} - {resultString}" };
            }
        }
        catch (Exception ex)
        {
            return new SmsResponse { IsSuccess = false, Message = $"خطای غیرمنتظره: {ex.Message}" };
        }    }

    public async Task<SmsResponse> AthleteSuccessfullySmsNotificationForBuyFromSite(string mobileNumber, string wpKey, string serviceName)
    {
        var message = 
            $"🏋️‍♂️ ورزشکار عزیز\n" +
            $"پرداخت شما برای سرویس «{serviceName}» با موفقیت انجام شد. 🎉\n\n" +
            $"لطفاً از طریق لینک زیر به سوالات مربی پاسخ دهید تا برنامه‌ی اختصاصی شما طراحی شود:\n" +
            $"chaarset.ir/program/{wpKey}/";
        const string lineNumber = "9981802897"; 
        const string apiUrl = "https://api.sms.ir/v1/send/likeToLike"; 

        var payload = new
        {
            LineNumber = lineNumber,
            MessageTexts = new[] { message },
            Mobiles = new[] { mobileNumber }
        };
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(apiUrl, content);
            var resultString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new SmsResponse { IsSuccess = true, Message = "پیامک با موفقیت ارسال شد." };
            }
            else
            {
              
                return new SmsResponse { IsSuccess = false, Message = $"خطا در ارسال پیامک: {response.StatusCode} - {resultString}" };
            }
        }
        catch (Exception ex)
        {
            return new SmsResponse { IsSuccess = false, Message = $"خطای غیرمنتظره: {ex.Message}" };
        }
    }
    public async Task<SmsResponse> WorkoutReadySms(string mobileNumber, string athleteName, string serviceName,string wpKey)
    {
        var message = $"{athleteName} عزیز، برنامه {serviceName} که منتظرش بودی آماده شد!\n" +
                      "همین الان به اپلیکیشن چارسِت برو و برنامه‌ات رو مشاهده کن.\n\n" +
                      $"chaarset.ir/program/{wpKey}/";

        const string lineNumber = "9981802897";
        const string apiUrl = "https://api.sms.ir/v1/send/likeToLike";


        var payload = new SmsPayload
        {
            LineNumber = lineNumber,
            MessageTexts = [message],
            Mobiles = [mobileNumber]
        };
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _accessKey);


        var jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(apiUrl, content);
            var resultString = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? new SmsResponse { IsSuccess = true, Message = resultString }
                : new SmsResponse { IsSuccess = false, Message = $"API Error: {response.StatusCode} - {resultString}" };
        }
        catch (Exception ex)
        {
            return new SmsResponse { IsSuccess = false, Message = $"An exception occurred: {ex.Message}" };
        }
    }
    public async Task<string> SendErrorSms(string message)
    {
        var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Add("x-api-key",
            _accessKey);

        var model = new VerifySendModel()
        {
            Mobile = "09395327229",
            TemplateId = 980201,
            Parameters =
            [
                new VerifySendParameterModel
                {
                    Name = "CODE", Value = message
                }
            ]
        };

        var payload = JsonSerializer.Serialize(model);
        StringContent stringContent = new(payload, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://api.sms.ir/v1/send/verify", stringContent);
        Console.WriteLine(response);
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

public class SmsPayload
{
    public string LineNumber { get; set; }
    public List<string> MessageTexts { get; set; }
    public List<string> Mobiles { get; set; }
    public DateTime? SendDateTime { get; set; } = null;
}

public class SmsResponse
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; }
}