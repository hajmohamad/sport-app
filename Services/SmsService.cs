using System.Text;
using System.Text.Json;
using sport_app_backend.Interface;

namespace sport_app_backend.Services;

public class SmsService (IConfiguration config) : ISmsService
{
    private readonly string _accessKey = config["SMS:accessKey"] ?? "string.Empty";

    public async Task<string> SendCode(string phoneNumber)
    {
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

    public async Task<string> SendErrorSms()
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
                    Name = "CODE", Value = "11111"
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
