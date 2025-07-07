using System.Text;
using Newtonsoft.Json;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Interface;

namespace sport_app_backend.Services;

public class ZarinPal :IZarinPal

{
    private static readonly HttpClient Client = new HttpClient();

    public  async Task<ZarinPalPaymentResponseDto> RequestPaymentAsync(ZarinPalPaymentRequestDto request)
    {
        var jsonData = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            var response =
                await Client.PostAsync("https://payment.zarinpal.com/pg/v4/payment/request.json", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            if (result?.data == null || result?.data.code != 100)
                return new ZarinPalPaymentResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = result?.errors?.message ?? "Unknown error"
                };
            var authority = result?.data.authority;
            var paymentUrl = $"https://payment.zarinpal.com/pg/StartPay/{authority}";

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

}