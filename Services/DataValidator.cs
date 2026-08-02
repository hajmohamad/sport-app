using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using sport_app_backend.Dtos.Eitaa;
using sport_app_backend.Interface;

namespace sport_app_backend.Services;

public class DataValidator(IConfiguration configuration) : IDataValidator
{
    public EitaaValidationResult Validate(string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData))
        {
            return Invalid("Data is empty.");
        }

        var botToken = configuration["Eitaa:BotToken"];

        if (string.IsNullOrWhiteSpace(botToken))
        {
            return Invalid("Eitaa bot token is not configured.");
        }

        var pairs = rawData.Split('&')
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new { Key = parts[0], Value = parts[1] })
            .ToList();

        var hashPair = pairs.FirstOrDefault(x => x.Key == "hash");
        if (hashPair == null)
        {
            return Invalid("Hash is missing.");
        }

        var receivedHash = Uri.UnescapeDataString(hashPair.Value).ToLowerInvariant();

     
        var dataCheckString = string.Join("\n", pairs
            .Where(x => x.Key != "hash")
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}={Uri.UnescapeDataString(x.Value)}"));

        var calculatedHash = CalculateHash(dataCheckString, botToken);

        if (!FixedTimeEqualsHex(calculatedHash, receivedHash))
        {
            var calculatedHashAlternative = CalculateHashAlternative(dataCheckString, botToken);
            if (FixedTimeEqualsHex(calculatedHashAlternative, receivedHash))
            {
                return new EitaaValidationResult
                {
                    IsValid = true,
                    Data = ParseToDictionary(rawData)
                };
            }

            return Invalid("Invalid hash.");
        }

        return new EitaaValidationResult
        {
            IsValid = true,
            Data = ParseToDictionary(rawData)
        };
    }

    // روش استاندارد (استفاده از بایت‌های خام به عنوان کلید مرحله دوم)
    private static string CalculateHash(string dataCheckString, string botToken)
    {
        byte[] secretKey;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData")))
        {
            secretKey = hmac.ComputeHash(Encoding.UTF8.GetBytes(botToken));
        }

        using (var hmac = new HMACSHA256(secretKey))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    // روش جایگزین (مخصوص فرمول ارائه شده در مستندات ایتا که از رشته Hex کلید می‌سازد)
    private static string CalculateHashAlternative(string dataCheckString, string botToken)
    {
        byte[] secretKeyBytes;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData")))
        {
            secretKeyBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(botToken));
        }
        
        // تبدیل بایت‌ها به رشته هگزادسیمال متنی طبق مستندات ایتا
        var secretKeyHex = Convert.ToHexString(secretKeyBytes).ToLowerInvariant();

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKeyHex)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    private static Dictionary<string, StringValues> ParseToDictionary(string rawData)
    {
        return QueryHelpers.ParseQuery(rawData);
    }

    private static bool FixedTimeEqualsHex(string leftHex, string rightHex)
    {
        try
        {
            var left = Convert.FromHexString(leftHex);
            var right = Convert.FromHexString(rightHex);

            return left.Length == right.Length &&
                   CryptographicOperations.FixedTimeEquals(left, right);
        }
        catch
        {
            return false;
        }
    }

    private static EitaaValidationResult Invalid(string error)
    {
        return new EitaaValidationResult
        {
            IsValid = false,
            Error = error
        };
    }
}
