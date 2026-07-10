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

        Dictionary<string, StringValues> parsed;

        try
        {
            parsed = QueryHelpers.ParseQuery(rawData);
        }
        catch
        {
            return Invalid("Invalid query string format.");
        }

        if (!parsed.TryGetValue("hash", out var receivedHashValues))
        {
            return Invalid("Hash is missing.");
        }

        var receivedHash = receivedHashValues.ToString();

        if (string.IsNullOrWhiteSpace(receivedHash))
        {
            return Invalid("Hash is empty.");
        }

        var dataCheckString = string.Join("\n", parsed
            .Where(x => !string.Equals(x.Key, "hash", StringComparison.Ordinal))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}={x.Value}"));

        var calculatedHash = CalculateHash(dataCheckString, botToken);

        if (!FixedTimeEqualsHex(calculatedHash, receivedHash))
        {
            return Invalid("Invalid hash.");
        }

        return new EitaaValidationResult
        {
            IsValid = true,
            Data = parsed
        };
    }

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
