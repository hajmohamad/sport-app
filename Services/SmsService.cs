using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using sport_app_backend.Interface;

namespace sport_app_backend.Services;

public class SmsResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
}

public class SmsService(IConfiguration config, ILogger<SmsService> logger) : ISmsService
{
    private readonly string _accessToken = config["SMS:accessKey"] ?? "deployMode";
    private readonly bool _isDeployMode = (config["SMS:accessKey"] ?? "deployMode") == "deployMode";

    private const string LineNumber = "9981802897";
    private const string LikeToLikeUrl = "https://api.sms.ir/v1/send/likeToLike";
    private const string VerifyUrl = "https://api.sms.ir/v1/send/verify";
    private readonly HttpClient _httpClient = new();

    private async Task<SmsResponse> SendLikeToLikeSms(string phoneNumber, string message)
    {
        logger.LogInformation(
            "Starting LikeToLike SMS send. PhoneNumber: {PhoneNumber}, DeployMode: {DeployMode}",
            phoneNumber,
            _isDeployMode);

        if (_isDeployMode)
        {
            logger.LogInformation(
                "SMS send skipped because application is in deploy mode. PhoneNumber: {PhoneNumber}",
                phoneNumber);

            return new SmsResponse
            {
                IsSuccess = true,
                Message = "deploy mode"
            };
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _accessToken);

            var payload = new
            {
                LineNumber,
                MessageTexts = new[] { message },
                Mobiles = new[] { phoneNumber }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            logger.LogInformation(
                "Sending LikeToLike SMS request to provider. PhoneNumber: {PhoneNumber}, Url: {Url}",
                phoneNumber,
                LikeToLikeUrl);

            var response = await _httpClient.PostAsync(LikeToLikeUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "LikeToLike SMS sent successfully. PhoneNumber: {PhoneNumber}, StatusCode: {StatusCode}, Response: {Response}",
                    phoneNumber,
                    (int)response.StatusCode,
                    result);
            }
            else
            {
                logger.LogWarning(
                    "LikeToLike SMS send failed. PhoneNumber: {PhoneNumber}, StatusCode: {StatusCode}, Response: {Response}",
                    phoneNumber,
                    (int)response.StatusCode,
                    result);
            }

            return new SmsResponse
            {
                IsSuccess = response.IsSuccessStatusCode,
                Message = result
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Exception occurred while sending LikeToLike SMS. PhoneNumber: {PhoneNumber}",
                phoneNumber);

            return new SmsResponse
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task<string> SendCode(string phoneNumber)
    {
        logger.LogInformation(
            "Starting verification code send. PhoneNumber: {PhoneNumber}, DeployMode: {DeployMode}",
            phoneNumber,
            _isDeployMode);

        if (_isDeployMode)
        {
            logger.LogInformation(
                "Verification code send skipped because application is in deploy mode. PhoneNumber: {PhoneNumber}",
                phoneNumber);

            return "12345";
        }

        var random = new Random();
        var code = random.Next(10000, 100000).ToString();

        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _accessToken);

            var model = new VerifySendModel
            {
                Mobile = phoneNumber,
                TemplateId = 980201,
                Parameters = new[]
                {
                    new VerifySendParameterModel
                    {
                        Name = "CODE",
                        Value = code
                    }
                }
            };

            var payload = JsonConvert.SerializeObject(model);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            logger.LogInformation(
                "Sending verify SMS request. PhoneNumber: {PhoneNumber}, TemplateId: {TemplateId}, Url: {Url}",
                phoneNumber,
                model.TemplateId,
                VerifyUrl);

            var response = await _httpClient.PostAsync(VerifyUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Verify SMS sent successfully. PhoneNumber: {PhoneNumber}, StatusCode: {StatusCode}, Response: {Response}",
                    phoneNumber,
                    (int)response.StatusCode,
                    result);
            }
            else
            {
                logger.LogWarning(
                    "Verify SMS send failed. PhoneNumber: {PhoneNumber}, StatusCode: {StatusCode}, Response: {Response}",
                    phoneNumber,
                    (int)response.StatusCode,
                    result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Exception occurred while sending verify SMS. PhoneNumber: {PhoneNumber}",
                phoneNumber);
        }

        return code;
    }

    public async Task<string> SiteLogin(string phoneNumber)
    {
        logger.LogInformation(
            "Starting site login SMS send. PhoneNumber: {PhoneNumber}, DeployMode: {DeployMode}",
            phoneNumber,
            _isDeployMode);

        if (_isDeployMode)
        {
            logger.LogInformation(
                "Site login SMS skipped because application is in deploy mode. PhoneNumber: {PhoneNumber}",
                phoneNumber);

            return "12345";
        }

        var random = new Random();
        var code = random.Next(10000, 100000).ToString();

        var message =
            "کد ورود به سایت بدنسازی چارسِت\n" +
            $"Code:{code}\n" +
            "Chaarset.ir";

        var response = await SendLikeToLikeSms(phoneNumber, message);

        if (response.IsSuccess)
        {
            logger.LogInformation(
                "Site login SMS sent successfully. PhoneNumber: {PhoneNumber}",
                phoneNumber);
        }
        else
        {
            logger.LogWarning(
                "Site login SMS failed. PhoneNumber: {PhoneNumber}, ProviderMessage: {ProviderMessage}",
                phoneNumber,
                response.Message);
        }

        return code;
    }

    public async Task<SmsResponse> SupportTicketCreatedSms(string mobileNumber, string ticketTitle)
    {
        logger.LogInformation(
            "Preparing SupportTicketCreatedSms. PhoneNumber: {PhoneNumber}, TicketTitle: {TicketTitle}",
            mobileNumber,
            ticketTitle);

        var message = $"کاربر گرامی، تیکت شما با موضوع «{ticketTitle}» در چارسِت ایجاد شد.";
        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> SupportTicketAnsweredSms(string mobileNumber, string ticketTitle)
    {
        logger.LogInformation(
            "Preparing SupportTicketAnsweredSms. PhoneNumber: {PhoneNumber}, TicketTitle: {TicketTitle}",
            mobileNumber,
            ticketTitle);

        var message = $"کاربر گرامی، تیکت شما با موضوع «{ticketTitle}» در چارسِت پاسخ داده شد.";
        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> CoachServiceBuySmsNotification(string phoneNumber, string coachName, string serviceName, string price)
    {
        logger.LogInformation(
            "Preparing CoachServiceBuySmsNotification. PhoneNumber: {PhoneNumber}, CoachName: {CoachName}, ServiceName: {ServiceName}",
            phoneNumber,
            coachName,
            serviceName);

        var message =
            $"{coachName} عزیز، یک نفر {serviceName} رو ازت خریداری کرد.\n" +
            $"میتونی همین الان در عرض چند دقیقه برنامه رو طراحی و مبلغ {price} تومان رو دریافت کنی.\n\n" +
            "chaarset.ir";

        return await SendLikeToLikeSms(phoneNumber, message);
    }

    public async Task<SmsResponse> AthleteSuccessfullySmsNotification(string mobileNumber, string athleteName, string serviceName)
    {
        logger.LogInformation(
            "Preparing AthleteSuccessfullySmsNotification. PhoneNumber: {PhoneNumber}, AthleteName: {AthleteName}, ServiceName: {ServiceName}",
            mobileNumber,
            athleteName,
            serviceName);

        var message =
            $"{athleteName} عزیز، درخواستت برای برنامه {serviceName} با موفقیت برای مربی ارسال شد.\n" +
            "برای مشاهده وضعیت برنامه وارد اپلیکیشن چارسِت شو.\n\n" +
            "chaarset.ir";

        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> NotifyAthleteOfProgramLinkSms(string mobileNumber, string athleteName, string link)
    {
        logger.LogInformation(
            "Preparing NotifyAthleteOfProgramLinkSms. PhoneNumber: {PhoneNumber}, AthleteName: {AthleteName}",
            mobileNumber,
            athleteName);

        var message =
            $"{athleteName} عزیز، درخواست شما برای مربی ارسال شد.\n" +
            $"chaarset.ir/program/{link}/";

        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> AthleteSuccessfullySmsNotificationForBuyFromSite(string mobileNumber,
        string serviceName, string wpKey)
    {
        logger.LogInformation(
            "Preparing AthleteSuccessfullySmsNotificationForBuyFromSite. PhoneNumber: {PhoneNumber}, ServiceName: {ServiceName}",
            mobileNumber,
            serviceName);

        var message =
            "🏋️‍♂️ ورزشکار عزیز\n" +
            "پرداخت شما برای سرویس «برنامه تمرینی» با موفقیت انجام شد. 🎉\n\n" +
            "لطفاً از طریق لینک زیر به سوالات مربی پاسخ دهید تا برنامه‌ی اختصاصی شما طراحی شود:\n" +
            $"chaarset.ir/program/{wpKey}/";
        
        return await SendLikeToLikeSms(mobileNumber, message);
    }

    public async Task<SmsResponse> WorkoutReadySms(string mobileNumber, string athleteName, string serviceName, string wpKey)
    {
        logger.LogInformation(
            "Preparing WorkoutReadySms. PhoneNumber: {PhoneNumber}, AthleteName: {AthleteName}, ServiceName: {ServiceName}",
            mobileNumber,
            athleteName,
            serviceName);

        var message =
            $"{athleteName} عزیز، برنامه {serviceName} که منتظرش بودی آماده شد!\n" +
            "همین الان به اپلیکیشن چارسِت برو و برنامه‌ات رو مشاهده کن.\n\n" +
            $"chaarset.ir/program/{wpKey}/";

        return await SendLikeToLikeSms(mobileNumber, message);
    }


    public async Task<SmsResponse> SendPaymentAttemptSms(string phoneNumber, string serviceTitle, string coachName, string websiteUrl)
    {
        logger.LogInformation(
            "Preparing SendPaymentAttemptSms. PhoneNumber: {PhoneNumber}, ServiceTitle: {ServiceTitle}, CoachName: {CoachName}",
            phoneNumber,
            serviceTitle,
            coachName);

        var message =
            $"فقط یک قدم تا دریافت {serviceTitle} از {coachName} باقی مونده!\n" +
            $"برای نهایی کردن درخواستت، از اینجا ادامه بده:\n" +
            $"{websiteUrl}";

        return await SendLikeToLikeSms(phoneNumber, message);
    }

    public async Task<SmsResponse> SendProgramExpiredReminderSms(string phoneNumber, string athleteName, int daysSinceEnd, string websiteUrl)
    {
        logger.LogInformation(
            "Preparing SendProgramExpiredReminderSms. PhoneNumber: {PhoneNumber}, AthleteName: {AthleteName}, DaysSinceEnd: {DaysSinceEnd}",
            phoneNumber,
            athleteName,
            daysSinceEnd);

        var message =
            $"{athleteName} عزیز، {daysSinceEnd} روز از آخرین برنامه تمرینی که دریافت کردی گذشته.\n" +
            $"برای دریافت برنامه جدیدت از لینک زیر به مربی خودت درخواست بده:\n" +
            $"{websiteUrl}";

        return await SendLikeToLikeSms(phoneNumber, message);
    }

    public async Task<SmsResponse> SendProgramSessionsReminderSms(string phoneNumber, string athleteName, int remainingSessions, string websiteUrl)
    {
        logger.LogInformation(
            "Preparing SendProgramSessionsReminderSms. PhoneNumber: {PhoneNumber}, AthleteName: {AthleteName}, RemainingSessions: {RemainingSessions}",
            phoneNumber,
            athleteName,
            remainingSessions);

        var message =
            $"{athleteName} عزیز، کمتر از {remainingSessions} جلسه از برنامه تمرینیت باقی مونده.\n" +
            $"برای دریافت برنامه جدیدت از لینک زیر به مربی خودت درخواست بده:\n" +
            $"{websiteUrl}";

        return await SendLikeToLikeSms(phoneNumber, message);
    }

    public async Task<SmsResponse> SendQuestionReminderSms(string phoneNumber, string athleteName, string link)
    {
        logger.LogInformation(
            "Preparing SendQuestionReminderSms. PhoneNumber: {PhoneNumber}, AthleteName: {AthleteName}",
            phoneNumber,
            athleteName);

        var message =
            $"{athleteName} عزیز، فرم اطلاعات اولیه‌ای که برای دریافت برنامه خریده بودی هنوز تکمیل نشده.\n" +
            $"لطفا از طریق لینک زیر فرم رو کامل کن:\n" +
            $"{link}";

        return await SendLikeToLikeSms(phoneNumber, message);
    }

    public async Task<string> SendErrorSms(string message)
    {
        logger.LogInformation(
            "Starting SendErrorSms. DeployMode: {DeployMode}",
            _isDeployMode);

        if (_isDeployMode)
        {
            logger.LogInformation("SendErrorSms skipped because application is in deploy mode.");
            return "00000";
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _accessToken);

            var model = new VerifySendModel
            {
                Mobile = "09395327229",
                TemplateId = 980201,
                Parameters = new[]
                {
                    new VerifySendParameterModel
                    {
                        Name = "CODE",
                        Value = message
                    }
                }
            };

            var payload = JsonConvert.SerializeObject(model);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            logger.LogInformation(
                "Sending error SMS to admin. TemplateId: {TemplateId}, Url: {Url}",
                model.TemplateId,
                VerifyUrl);

            var response = await _httpClient.PostAsync(VerifyUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Error SMS sent successfully. StatusCode: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    result);
            }
            else
            {
                logger.LogWarning(
                    "Error SMS send failed. StatusCode: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while sending error SMS.");
        }

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
