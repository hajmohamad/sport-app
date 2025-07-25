using System.Text;
using Newtonsoft.Json;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Payments;

namespace sport_app_backend.Services;

public class ZarinPal(IConfiguration config) : IZarinPal

{
    private readonly string _merchantId = config["Zarinpal:MerchantId"] ?? "string.Empty";
    private readonly string _endpoint = config["Zarinpal:endpoint"] ?? "string.Empty";


    private static readonly HttpClient Client = new HttpClient();

    public  async Task<ZarinPalPaymentResponseDto> RequestPaymentAsync(ZarinPalPaymentRequestDto request)
    {
        request.merchant_id = _merchantId;
        var jsonData = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            var response =
                await Client.PostAsync(_endpoint+"pg/v4/payment/request.json", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            if (result?.data == null || result?.data.code != 100)
                return new ZarinPalPaymentResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = result?.errors?.message ?? "Unknown error"
                };
            var authority = result?.data.authority;
            var paymentUrl = _endpoint+$"pg/StartPay/{authority}";

            return new ZarinPalPaymentResponseDto
            {
                PaymentUrl = paymentUrl,
                Authority = authority,
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            return new ZarinPalPaymentResponseDto
            {
                IsSuccessful = false,
                ErrorMessage = $"Error sending payment request: {ex.Message}"
            };
        }
        
    }

    public async Task<ZarinpalVerifyApiResponseDto> VerifyPaymentAsync(ZarinPalVerifyRequestDto request)
    {
        var data = new
        {
            merchant_id = _merchantId,
            authority = request.Authority,
            amount =  request.Amount,
            currency = "IRT"
        };

        var jsonData = JsonConvert.SerializeObject(data);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

      
        var response =
                await Client.PostAsync(_endpoint+"pg/v4/payment/verify.json", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ZarinpalVerifyApiResponseDto>(responseContent);
        return result;
      
    }

}