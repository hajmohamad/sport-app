using System.Text.RegularExpressions;

namespace sport_app_backend.Mappers;

public static class PhoneNumberHelper
{
    public static string NormalizeIranPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is empty.");

        var value = phoneNumber.Trim();

        value = value.Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "");

        value = ToEnglishDigits(value);

        if (value.StartsWith("+98"))
            value = "0" + value[3..];

        if (value.StartsWith("0098"))
            value = "0" + value[4..];

        if (value.StartsWith("98") && value.Length == 12)
            value = "0" + value[2..];

        if (value.StartsWith("9") && value.Length == 10)
            value = "0" + value;

        if (!Regex.IsMatch(value, @"^09\d{9}$"))
            throw new ArgumentException("Invalid Iranian phone number.");

        return value;
    }

    private static string ToEnglishDigits(string input)
    {
        return input
            .Replace('۰', '0')
            .Replace('۱', '1')
            .Replace('۲', '2')
            .Replace('۳', '3')
            .Replace('۴', '4')
            .Replace('۵', '5')
            .Replace('۶', '6')
            .Replace('۷', '7')
            .Replace('۸', '8')
            .Replace('۹', '9')
            .Replace('٠', '0')
            .Replace('١', '1')
            .Replace('٢', '2')
            .Replace('٣', '3')
            .Replace('٤', '4')
            .Replace('٥', '5')
            .Replace('٦', '6')
            .Replace('٧', '7')
            .Replace('٨', '8')
            .Replace('٩', '9');
    }
}